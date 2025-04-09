using System.Collections.Generic;

namespace src.Data
{
    public class TaskNode
    {
        public string Context { get; set; }
        public List<TaskNode> SubNodes { get; set; }

        public TaskNode(string context)
        {
            Context = context;
            SubNodes = new List<TaskNode>();
        }

        public void AddSubNode(TaskNode subNode)
        {
            SubNodes.Add(subNode);
        }

        public void ProcessTask(string task)
        {
            // Process the task
        }
    }
}
