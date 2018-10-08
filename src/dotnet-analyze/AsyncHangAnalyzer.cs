using System.Collections.Generic;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.Tools.Analyze
{
    internal class AsyncHangAnalyzer
    {
        internal const int TASK_STATE_STARTED = 0x10000;                                       //bin: 0000 0000 0000 0001 0000 0000 0000 0000
        internal const int TASK_STATE_DELEGATE_INVOKED = 0x20000;                              //bin: 0000 0000 0000 0010 0000 0000 0000 0000
        internal const int TASK_STATE_DISPOSED = 0x40000;                                      //bin: 0000 0000 0000 0100 0000 0000 0000 0000
        internal const int TASK_STATE_EXCEPTIONOBSERVEDBYPARENT = 0x80000;                     //bin: 0000 0000 0000 1000 0000 0000 0000 0000
        internal const int TASK_STATE_CANCELLATIONACKNOWLEDGED = 0x100000;                     //bin: 0000 0000 0001 0000 0000 0000 0000 0000
        internal const int TASK_STATE_FAULTED = 0x200000;                                      //bin: 0000 0000 0010 0000 0000 0000 0000 0000
        internal const int TASK_STATE_CANCELED = 0x400000;                                     //bin: 0000 0000 0100 0000 0000 0000 0000 0000
        internal const int TASK_STATE_WAITING_ON_CHILDREN = 0x800000;                          //bin: 0000 0000 1000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_RAN_TO_COMPLETION = 0x1000000;                           //bin: 0000 0001 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_WAITINGFORACTIVATION = 0x2000000;                        //bin: 0000 0010 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_COMPLETION_RESERVED = 0x4000000;                         //bin: 0000 0100 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_THREAD_WAS_ABORTED = 0x8000000;                          //bin: 0000 1000 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_WAIT_COMPLETION_NOTIFICATION = 0x10000000;               //bin: 0001 0000 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_EXECUTIONCONTEXT_IS_NULL = 0x20000000;                   //bin: 0010 0000 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_TASKSCHEDULED_WAS_FIRED = 0x40000000;                    //bin: 0100 0000 0000 0000 0000 0000 0000 0000

        public static void Run(IConsole console, ClrRuntime runtime)
        {
            // Collect all tasks
            var waitingTasks = new List<(ClrObject, TaskStatus)>();
            foreach (var obj in runtime.Heap.EnumerateObjects())
            {
                if (obj.Type.IsDerivedFrom("System.Threading.Tasks.Task"))
                {
                    var state = obj.GetField<int>("m_stateFlags");
                    var status = ToTaskStatus(state);
                    if (status != TaskStatus.Faulted && status != TaskStatus.Canceled && status != TaskStatus.RanToCompletion)
                    {
                        waitingTasks.Add((obj, status));
                    }
                }
            }

            foreach (var (task, status) in waitingTasks)
            {
                console.WriteLine($"* {task.Type.Name} - {status}");
            }
        }

        private static TaskStatus ToTaskStatus(int stateFlags)
        {
            TaskStatus rval;

            if ((stateFlags & TASK_STATE_FAULTED) != 0)
            {
                rval = TaskStatus.Faulted;
            }
            else if ((stateFlags & TASK_STATE_CANCELED) != 0)
            {
                rval = TaskStatus.Canceled;
            }
            else if ((stateFlags & TASK_STATE_RAN_TO_COMPLETION) != 0)
            {
                rval = TaskStatus.RanToCompletion;
            }
            else if ((stateFlags & TASK_STATE_WAITING_ON_CHILDREN) != 0)
            {
                rval = TaskStatus.WaitingForChildrenToComplete;
            }
            else if ((stateFlags & TASK_STATE_DELEGATE_INVOKED) != 0)
            {
                rval = TaskStatus.Running;
            }
            else if ((stateFlags & TASK_STATE_STARTED) != 0)
            {
                rval = TaskStatus.WaitingToRun;
            }
            else if ((stateFlags & TASK_STATE_WAITINGFORACTIVATION) != 0)
            {
                rval = TaskStatus.WaitingForActivation;
            }
            else
            {
                rval = TaskStatus.Created;
            }

            return rval;
        }
    }
}
