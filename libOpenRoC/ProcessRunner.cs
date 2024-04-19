namespace liboroc
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Timers;
	using Signal = System.Threading.ManualResetEventSlim;

    /// <summary>
    /// The central nerve system of OpenRoC! Handles restarting,
    /// closing, monitoring, and etc. of a running Process. API
    /// of this class is not thread-safe unless mentioned otherwise
    /// </summary>
    public class ProcessRunner : IDisposable, INotifyPropertyChanged
	{
		private Status currentState;
		private Status previousState;
		private Signal crashSignal;
		private Signal startSignal;
		private Signal checkSignal;
		private Signal resetTimer;
		private Timer gracePeriodTimer;
		private Timer doubleCheckTimer;
		private ProcessOptions options;

        /// <summary>
        /// Event propagated when Process state changes (e.g from Stopped to Running)
        /// This is called from the UI thread and other threads
        /// </summary>
        public Action StateChanged;

        /// <summary>
        /// Event propagated when Process options changes (e.g launch options)
        /// /// This is called from the UI thread
        /// </summary>
        public Action OptionsChanged;

        /// <summary>
        /// Event propagated when Process object is re-assigned internally
        /// All accesses to ProcessRunner.Process should be renewed after
        /// This is called from the UI thread and other threads
        /// </summary>
        public Action ProcessChanged;

        /// <summary>
        /// Event propagated when Process is crashed or stopped
        /// This is called from the UI thread
        /// </summary>
        public Action ProcessCrashed;

        /// <summary>
        /// Current status of the monitored Process
        /// Running: Process is all well and running correctly
        /// Disabled: Process is not being monitored
        /// Stopped: Process is stopped and not running
        /// Invalid: Intermediate step, for internal use only
        /// </summary>
        public enum Status
		{
			Running = 0,
			Disabled = 1,
			Stopped = 2,
			Invalid = 3
		}

        /// <summary>
        /// Current state of the Process. Assigning a new State will be
        /// in effect the next time ProcessRunner.Monitor is called
        /// </summary>
        public Status State
		{
			get { return currentState; }
			set
			{
				if (value == State)
				{
					return;
				}

				options.InitialStateEnumValue = value;
				previousState = currentState;
				currentState = value;
				ResetTimers();

				NotifyPropertyChanged(nameof(State));
			}
		}

        /// <summary>
        /// Native Process object. External accesses to this getter needs
        /// to be renewed on propagation of ProcessRunner.ProcessChanged
        /// </summary>
        public Process Process { get; private set; }

        /// <summary>
        /// A Stopwatch object holding the running time since the last time
        /// ProcessRunner.State has been re-assigned or changed internally
        /// </summary>
        public Stopwatch Stopwatch { get; private set; }

        /// <summary>
        /// Returns true if a Process has a GUI and false for Console based
        /// </summary>
        public bool HasWindow { get; private set; } = false;

        /// <summary>
        /// Launch options used to instantiate ProcessRunner.Process with
        /// </summary>
        public ProcessOptions ProcessOptions
		{
			get { return options.Clone() as ProcessOptions; }
			set { SwapOptions(value.Clone() as ProcessOptions); }
		}

		public ProcessRunner(ProcessOptions opts)
		{
			Stopwatch = new Stopwatch();
			currentState = opts.InitialStateEnumValue;
			previousState = Status.Invalid;
			resetTimer = new Signal(true);
			startSignal = new Signal(false);
			checkSignal = new Signal(false);
			crashSignal = new Signal(false);
			options = opts.Clone() as ProcessOptions;
			gracePeriodTimer = new Timer { AutoReset = false, Enabled = false };
			doubleCheckTimer = new Timer { AutoReset = false, Enabled = false };
			gracePeriodTimer.Elapsed += OnGracePeriodTimeElapsed;
			doubleCheckTimer.Elapsed += OnDoubleCheckTimeElapsed;
			SetupOptions();
		}

        /// <summary>
        /// Restores the ProcessRunner.State to the previous one. It has
        /// only memory for ONE single past state. Multiple calls are invalid
        /// </summary>
        public void RestoreState()
		{
			State = previousState == Status.Invalid ? Status.Stopped : previousState;
		}

        /// <summary>
        /// Starts the ProcessRunner.Process immediately. It stops the Process
        /// if it's already in the Running state
        /// </summary>
        public void Start()
		{
			if (Process != null)
			{
				Stop();
			}

			if (options.PreLaunchScriptEnabled)
			{
				ProcessHelper.ExecuteScript(options.PreLaunchScriptPath);
			}

			ProcessStartInfo sinfo = new ProcessStartInfo
			{
				WorkingDirectory = options.WorkingDirectory,
				FileName = options.Path,
				UseShellExecute = false
			};

			if (options.CommandLineEnabled &&
				!string.IsNullOrWhiteSpace(options.CommandLine))
			{
				sinfo.Arguments = options.CommandLine;
			}

			if (options.EnvironmentVariablesEnabled &&
				options.EnvironmentVariablesDictionary.Count > 0)
			{
				foreach (KeyValuePair<string, string> pair in options.EnvironmentVariablesDictionary)
				{
					sinfo.EnvironmentVariables.Add(pair.Key, pair.Value);
				}
			}

			Process = new Process
			{
				StartInfo = sinfo,
				EnableRaisingEvents = true
			};

			Process.Disposed += OnProcessStopped;
			Process.Exited += OnProcessStopped;

			Process.Start();
			Process.Refresh();

			try
			{
				Process.WaitForInputIdle();
				HasWindow = true;
			}
			catch (Exception)
			{
				HasWindow = false;
			}

			if (!HasWindow || Process.Responding)
			{
				State = Status.Invalid;
				State = Status.Running;

				NotifyPropertyChanged(nameof(Process));
			}
			else
			{
				Stop();
			}
		}

        /// <summary>
        /// Stops the ProcessRunner.Process immediately. No-op if already stopped.
        /// </summary>
        public void Stop()
		{
			if (Process == null)
			{
				if (!IsDisposed && State != Status.Stopped)
				{
					State = Status.Stopped;
				}

				return;
			}

			HasWindow = false;
			gracePeriodTimer.Stop();
			doubleCheckTimer.Stop();

			Process.Refresh();

			Process.EnableRaisingEvents = false;
			Process.Disposed -= OnProcessStopped;
			Process.Exited -= OnProcessStopped;

			int process_id = 0;
			var process_exit_timeout = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;

			if (!Process.HasExited)
			{
				process_id = Process.Id;

				Process.CloseMainWindow();
				Process.WaitForExit(process_exit_timeout);

				if (options.AggressiveCleanupEnabled)
				{
					ProcessHelper.Shutdown(process_id);
				}

				if (!Process.HasExited)
				{
					Process.Kill();
					Process.WaitForExit(process_exit_timeout);
				}
			}

			Process.Dispose();
			Process = null;

			NotifyPropertyChanged(nameof(Process));

			if (!IsDisposed)
			{
				State = Status.Invalid;
				State = Status.Stopped;
			}

			if (options.PostCrashScriptEnabled)
			{
				ProcessHelper.ExecuteScript(options.PostCrashScriptPath);
			}
		}

        /// <summary>
        /// Monitoring check routine (check for crashes and etc.)
        /// This needs to be called as often as possible in UI thread
        /// </summary>
        public void Monitor()
		{
			if (crashSignal.IsSet)
			{
				ProcessCrashed?.Invoke();

				Stop();

				if (options.GracePeriodEnabled)
				{
					if (!gracePeriodTimer.Enabled)
					{
						gracePeriodTimer.Start();
					}
				}
				else
				{
					startSignal.Set();
				}

				crashSignal.Reset();
			}

			if (resetTimer.IsSet)
			{
				Stopwatch.Restart();
				resetTimer.Reset();
			}

			if (ShouldStart)
			{
				if (startSignal.IsSet)
				{
					startSignal.Reset();
				}

				Start();
			}

			if (previousState == Status.Running && Process != null && !Process.HasExited)
			{
				if (currentState != Status.Running)
				{
					Status previousStateSnapshot = previousState;
					Status currentStateSnapshot = currentState;

					Stop();

					State = previousStateSnapshot;
					State = currentStateSnapshot;
				}
			}
			else if (previousState == Status.Stopped && Process == null)
			{
				if (currentState == Status.Running)
				{
					Start();
				}
			}
			else if (previousState == Status.Disabled && Process == null)
			{
				if (currentState == Status.Running)
				{
					Start();
				}
			}

			if (currentState != Status.Disabled && HasWindow && Process != null && !Process.HasExited)
			{
				Process.Refresh();

				if (options.CrashedIfUnresponsive && !Process.Responding)
				{
					if (options.DoubleCheckEnabled)
					{
						if (checkSignal.IsSet)
						{
							if (!Process.Responding)
							{
								startSignal.Set();
							}

							checkSignal.Reset();
						}
						else if (!doubleCheckTimer.Enabled)
						{
							doubleCheckTimer.Start();
						}
					}
					else
					{
						startSignal.Set();
					}

					if (startSignal.IsSet)
					{
						Stop();
					}
				}

				if (options.AlwaysOnTopEnabled && Process != null && Process.Responding)
				{
					BringToFront(true);
				}
			}
		}

        /// <summary>
        /// Brings the process windows to front
        /// </summary>
        /// <param name="aggressive">brings all child windows to top as well</param>
        public void BringToFront(bool aggressive = false)
		{
			if (Process == null)
			{
				return;
			}

			ProcessHelper.BringToFront(Process.Id, aggressive);
		}

        private void SwapOptions(ProcessOptions opts)
        {
            Stop();
            options = opts;
            State = opts.InitialStateEnumValue;
            SetupOptions();

            NotifyPropertyChanged(nameof(ProcessOptions));
        }

        private void SetupOptions()
        {
            if (currentState == Status.Invalid)
            {
                currentState = Status.Stopped;
            }

            if (options.InitialStateEnumValue == Status.Running)
            {
                startSignal.Set();
            }

            if (options.GracePeriodEnabled)
            {
                gracePeriodTimer.Interval = TimeSpan.FromSeconds(options.GracePeriodDuration).TotalMilliseconds;
            }

            if (options.DoubleCheckEnabled)
            {
                doubleCheckTimer.Interval = TimeSpan.FromSeconds(options.DoubleCheckDuration).TotalMilliseconds;
            }

            ResetTimers();
        }

        private void ResetTimers()
		{
			resetTimer.Set();
			gracePeriodTimer.Stop();
			doubleCheckTimer.Stop();
		}

		private bool ShouldStart
		{
			get
			{
				return (Process == null) &&
				  (startSignal.IsSet ||
				  (State == Status.Running &&
				  options.CrashedIfNotRunning &&
				  !gracePeriodTimer.Enabled));
			}
		}

		#region Event callbacks

		private void OnProcessStopped(object sender, EventArgs e)
		{
			crashSignal.Set();
			gracePeriodTimer.Stop();
		}

		private void OnDoubleCheckTimeElapsed(object sender, ElapsedEventArgs e)
		{
			checkSignal.Set();
			doubleCheckTimer.Stop();
		}

		private void OnGracePeriodTimeElapsed(object sender, ElapsedEventArgs e)
		{
			startSignal.Set();
			gracePeriodTimer.Stop();
		}

        #endregion

        #region INotifyPropertyChanged support

        //! @cond
        public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			if (propertyName == nameof(State))
			{
				StateChanged?.Invoke();
			}
			else if (propertyName == nameof(ProcessOptions))
			{
				OptionsChanged?.Invoke();
			}
			else if (propertyName == nameof(Process))
			{
				ProcessChanged?.Invoke();
			}
		}
        //! @endcond

        #endregion

        #region IDisposable Support

        //! @cond
        public bool IsDisposed { get; private set; } = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				IsDisposed = true;

				if (disposing)
				{
					ResetTimers();
					Stop();

					gracePeriodTimer?.Dispose();
					doubleCheckTimer?.Dispose();
					crashSignal?.Dispose();
					startSignal?.Dispose();
					checkSignal?.Dispose();
					resetTimer?.Dispose();
				}

				gracePeriodTimer = null;
				doubleCheckTimer = null;
				crashSignal = null;
				startSignal = null;
				checkSignal = null;
				resetTimer = null;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
        //! @endcond

        #endregion
    }
}