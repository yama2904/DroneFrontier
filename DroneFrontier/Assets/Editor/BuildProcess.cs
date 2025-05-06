using System.Diagnostics;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildProcess : IPostprocessBuildWithReport
{
    private const string DEPLOY_BAT = @"Tool\deploy.bat";

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        var info = new ProcessStartInfo();
        info.FileName = DEPLOY_BAT;
        info.CreateNoWindow = true;
        info.UseShellExecute = false;
        using (Process process = Process.Start(info))
        {
            process.WaitForExit();
        }
    }
}
