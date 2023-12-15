namespace Trdr.Async
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

        /// <summary>
        /// Runs a task which continues on a given scheduler.
        /// </summary>
        public static Task Run(Func<Task> func, TaskScheduler scheduler)
        {
            return
                Task<Task>.Factory.StartNew(
                    func,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    scheduler)
                    .Unwrap();
        }
    }
}