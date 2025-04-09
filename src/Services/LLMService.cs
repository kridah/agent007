using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace src.Services
{
    public class LLMService : Hub
    {
        private readonly ConcurrentDictionary<string, SubNode> subNodes = new();

        public async Task SendMessageToSubNode(string subNodeId, string message)
        {
            if (subNodes.TryGetValue(subNodeId, out var subNode))
            {
                await subNode.ProcessMessage(message);
            }
        }

        public async Task<string> CreateSubNode(string context)
        {
            var subNode = new SubNode(context);
            var subNodeId = Guid.NewGuid().ToString();
            subNodes[subNodeId] = subNode;
            await Clients.All.SendAsync("SubNodeCreated", subNodeId);
            return subNodeId;
        }

        public async Task RemoveSubNode(string subNodeId)
        {
            if (subNodes.TryRemove(subNodeId, out var subNode))
            {
                await Clients.All.SendAsync("SubNodeRemoved", subNodeId);
            }
        }
    }

    public class SubNode
    {
        public string Context { get; }

        public SubNode(string context)
        {
            Context = context;
        }

        public Task ProcessMessage(string message)
        {
            // Process the message and return a task
            return Task.CompletedTask;
        }
    }
}
