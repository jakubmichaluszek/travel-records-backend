namespace TravelRecordsAPI.Services
{
    public class ClearJpgService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = string.Empty;

            var parentDirectory = Directory.GetParent(workingDirectory);
            if (parentDirectory != null)
            {
                var grandParentDirectory = parentDirectory.Parent;
                if (grandParentDirectory != null)
                {
                    projectDirectory = grandParentDirectory.FullName;
                }
            }

            foreach (string sFile in Directory.GetFiles(workingDirectory, "*.jpg"))
            {
                try
                {
                    File.Delete(sFile);
                }
                catch (IOException)
                {
                    
                }
            }

            return Task.FromResult(0);
        }
    }
}
