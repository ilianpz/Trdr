namespace Trdr
{
    public static class TaskExtensions
    {
        public static async void Forget(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }
        }
    }
}
