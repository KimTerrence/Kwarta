namespace Kwarta.Views;

public partial class SplashOverlay : ContentView
{
    static readonly TaskCompletionSource Done = new();

    public SplashOverlay()
    {
        InitializeComponent();
    }

    /// <summary>Completes when the launch animation has finished. It plays once per app process.</summary>
    public static Task Finished => Done.Task;

    public static bool HasPlayed => Done.Task.IsCompleted;

    /// <summary>
    /// The logo settles with a short breath while <paramref name="loading"/> runs and then lifts away; the green
    /// then fades out, revealing the page underneath. <paramref name="onExit"/> runs just before that fade.
    /// </summary>
    public async Task PlayAsync(Task loading, Action onExit)
    {
        try
        {
            await Logo.ScaleToAsync(1.06, 280, Easing.SinOut);
            await Logo.ScaleToAsync(1.0, 260, Easing.SinInOut);

            try
            {
                // Usually done by now; never hold the app back for long.
                await loading.WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch (Exception)
            {
                // The page shows whatever it has; it loads again when it next appears.
            }

            await Task.WhenAll(
                Logo.ScaleToAsync(1.3, 320, Easing.CubicIn),
                Logo.FadeToAsync(0, 300, Easing.CubicIn));

            // Layout changes in onExit (the tab bar coming back) happen while the screen is plain green,
            // so nothing visibly jumps; then the green dissolves into the page.
            onExit();
            await Task.Delay(32);
            await FadeOutBackgroundAsync(380);
        }
        finally
        {
            IsVisible = false;
            Done.TrySetResult();
        }
    }

    Task FadeOutBackgroundAsync(uint length)
    {
        var from = BackgroundColor;
        var done = new TaskCompletionSource();
        new Animation(t => BackgroundColor = from.WithAlpha((float)(1 - t)))
            .Commit(this, "FadeOut", length: length, easing: Easing.CubicInOut, finished: (_, _) => done.TrySetResult());
        return done.Task;
    }
}
