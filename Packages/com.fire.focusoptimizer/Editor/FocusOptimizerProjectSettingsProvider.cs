using System.Collections.Generic;
using UnityEditor;

namespace FireTools.FocusOptimizer
{
    /// <summary>
    /// 将焦点卡顿优化助手注册到 Unity Project Settings。
    /// </summary>
    internal sealed class FocusOptimizerProjectSettingsProvider : SettingsProvider
    {
        private readonly FocusOptimizerSettingsView _view = new();

        private FocusOptimizerProjectSettingsProvider()
            : base("Project/Focus Optimizer", SettingsScope.Project)
        {
            label = FocusOptimizerLocalization.Tr("window.title");
            keywords = new HashSet<string>(new[]
            {
                "Focus", "刷新", "失焦", "卡顿", "Enter Play Mode"
            });
        }

        public override void OnGUI(string searchContext)
        {
            label = FocusOptimizerLocalization.Tr("window.title");
            _view.OnGUI();
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new FocusOptimizerProjectSettingsProvider();
        }
    }
}

























