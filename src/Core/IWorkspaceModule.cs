using System.Windows.Forms;

namespace WorkSpace.Core
{
    public interface IWorkspaceModule
    {
        string Title { get; }
        Control View { get; }
        void RefreshData();
    }
}
