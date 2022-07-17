namespace liboroc
{
	using System;
	using System.Diagnostics;
	using System.Management;


    /// <summary>
    /// A utility class that can query child Processes of a PID and
    /// executes a Visitor for each of them. Queries use WMI objects
    /// </summary>
    public class ProcessObserver
	{
		private static object mutex = new object();
		private static ProcessObserver instance;

        /// <summary>
        /// Single-instance to Observer
        /// </summary>
        public static ProcessObserver Instance
		{
			get
			{
				if (instance == null)
				{
					lock (mutex)
					{
						if (instance == null)
						{
							instance = new ProcessObserver();
						}
					}
				}

				return instance;
			}
		}

        /// <summary>
        /// Accepts a visitor to all queried Processes. Visitor should
        /// not Dispose the passed in Processes
        /// </summary>
        /// <param name="visitor">callback to be called for each found Process</param>
        /// <param name="pid">PID of the parent Process</param>
        public void Accept(Action<Process> visitor, int pid)
		{
			using (ExecutorService service = new ExecutorService())
			{
				service.Accept(() => { execute(visitor, pid); });
			}
		}

		private void execute(Action<Process> visitor, int pid)
		{
			string query = string.Format("Select * From Win32_Process Where ParentProcessID={0}", pid);
			using (ManagementObjectSearcher processSearcher = new ManagementObjectSearcher(query))
			using (ManagementObjectCollection processCollection = processSearcher.Get())
			{
				try
				{
					using (Process proc = Process.GetProcessById(pid))
					{
						visitor?.Invoke(proc);
					}
				}
				catch { /* there is nothing we can do about it */ }

				if (processCollection != null)
				{
					foreach (ManagementObject mo in processCollection)
					{
						execute(visitor, Convert.ToInt32(mo["ProcessID"]));
					}
				}
			}
		}
	}
}