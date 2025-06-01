using Agent007.Models;
using Microsoft.Extensions.Logging;

namespace Agent007.Data
{
    public interface IModelRepository
    {
        Task<IEnumerable<OllamaModel>> GetAvailableModelsAsync();
        Task<OllamaModel?> GetModelByNameAsync(string name);
        Task RefreshModelsAsync();
        Task<OllamaModel> StartDownloadAsync(string modelName, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
        Task DeleteModelAsync(string modelName);
        void CancelDownload(string modelName);

        // Events
        event EventHandler<ModelsChangedEventArgs>? ModelsChanged;
        event EventHandler<ModelProgressEventArgs>? ModelProgressChanged;
    }

    public class OllamaModelRepository : IModelRepository
    {
        private readonly OllamaService _ollamaService;
        private readonly ILogger<OllamaModelRepository> _logger;

        public OllamaModelRepository(OllamaService ollamaService, ILogger<OllamaModelRepository> logger)
        {
            _ollamaService = ollamaService;
            _logger = logger;

            // Forward events from service
            _ollamaService.ModelsChanged += (sender, args) => ModelsChanged?.Invoke(sender, args);
            _ollamaService.ModelProgressChanged += (sender, args) => ModelProgressChanged?.Invoke(sender, args);
        }

        public event EventHandler<ModelsChangedEventArgs>? ModelsChanged;
        public event EventHandler<ModelProgressEventArgs>? ModelProgressChanged;

        public Task<IEnumerable<OllamaModel>> GetAvailableModelsAsync()
        {
            return Task.FromResult<IEnumerable<OllamaModel>>(_ollamaService._models.Values);
        }

        public Task<OllamaModel?> GetModelByNameAsync(string name)
        {
            _ollamaService._models.TryGetValue(name, out var model);
            return Task.FromResult(model);
        }

        public async Task RefreshModelsAsync()
        {
            _logger.LogInformation("Refreshing models from Ollama service");
            await _ollamaService.ListModelsAsync();
        }

        public async Task<OllamaModel> StartDownloadAsync(string modelName, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting download for model: {ModelName}", modelName);
            return await _ollamaService.StartDownloadAsync(modelName, progress, cancellationToken);
        }

        public async Task DeleteModelAsync(string modelName)
        {
            _logger.LogInformation("Deleting model: {ModelName}", modelName);
            await _ollamaService.DeleteModelAsync(modelName);
        }

        public void CancelDownload(string modelName)
        {
            _logger.LogInformation("Cancelling download for model: {ModelName}", modelName);
            _ollamaService.CancelDownload(modelName);
        }
    }

    public class ModelsChangedEventArgs : EventArgs
    {
        public ModelsChangedType ChangeType { get; }
        public object Data { get; }

        public ModelsChangedEventArgs(ModelsChangedType changeType, object data)
        {
            ChangeType = changeType;
            Data = data;
        }
    }

    public class ModelProgressEventArgs : EventArgs
    {
        public string ModelName { get; }
        public float Progress { get; }

        public ModelProgressEventArgs(string modelName, float progress)
        {
            ModelName = modelName;
            Progress = progress;
        }
    }

    public enum ModelsChangedType
    {
        Added,
        Removed,
        Refreshed,
        StatusChanged
    }
}