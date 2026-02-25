using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

namespace SmartStandby.Services;

public class ToastNotificationService
{
    public void Show(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show toast notification.");
        }
    }
}
