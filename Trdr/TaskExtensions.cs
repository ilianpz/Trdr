namespace Trdr
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Forgets a <see cref="Task"/>. This will prevent <see cref="TaskScheduler.UnobservedTaskException"/> from
        /// being triggered if the Task threw.
        /// </summary>
        /// <param name="task"></param>
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