using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT
{
    public class SyncQueueManager
    {
        LinkedList<QueueTask> queueTasks = new LinkedList<QueueTask>();
        QueueTask? runningTask = null;
        public virtual int Add(QueueTask task)
        {
            lock (this)
            {
                if (queueTasks.Count == 0 && runningTask is null)
                {
                    CompleteTask(task);
                }
                else
                    queueTasks.AddLast(task);
                task.OnAdded(this, queueTasks.Count);
                return queueTasks.Count;
            }
        }
        protected virtual void CompleteTask(QueueTask task)
        {
            runningTask = task;
            Task.Run(() =>
            {
                task.OnComplete();
                lock (this)
                {
                    var nt = GetNextTask();
                    if (nt is null)
                        return;
                    CompleteTask(nt);
                }
            });
        }
        protected virtual QueueTask? GetNextTask()
        {
            if (queueTasks.Count == 0)
                return null;
            var task = queueTasks.First();
            queueTasks.RemoveFirst();
            return task;
        }
    }
    public class QueueTask
    {
        SyncQueueManager? parent;
        public virtual void OnAdded(SyncQueueManager parent, int index)
        {
            this.parent = parent;
        }
        public virtual void OnComplete()
        {

        }
        public virtual void OnDestroy()
        {

        }
        public virtual void OnCancel()
        {

        }
    }
}
