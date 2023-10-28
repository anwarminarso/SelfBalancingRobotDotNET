using SelfBalancingRobot.WebUI.Hubs;

namespace SelfBalancingRobot.WebUI.Models
{
    public abstract class BaseMonitoringContext
    {
        protected Task monitoringTask;
        protected CancellationTokenSource monitoringTaskToken;
        public virtual void StartMonitoring()
        {
            if (monitoringTask == null || monitoringTask.IsCanceled)
            {
                monitoringTaskToken = new CancellationTokenSource();
                OnStartMonitoring();
                monitoringTask = new Task(() =>
                {
                    while (!monitoringTaskToken.IsCancellationRequested)
                    {
                        MonitoringLoop();
                    }
                }, monitoringTaskToken.Token);
                monitoringTask.Start();
            }
        }
        public virtual void StopMonitoring()
        {
            if (monitoringTaskToken != null)
            {
                monitoringTaskToken.Cancel();
                while (monitoringTask.Status == TaskStatus.Running)
                {
                }
                if (monitoringTask != null)
                {
                    monitoringTask.Dispose();
                    monitoringTask = null;
                }
                monitoringTaskToken.Dispose();
                monitoringTaskToken = null;
            }
        }

        public virtual void OnStartMonitoring()
        {
        }
        public abstract void MonitoringLoop();
    }
}
