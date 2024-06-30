using System.Reflection;

namespace NAK.ASTExtension.Integrations
{
    public static partial class BtkUiAddon
    {
        #region Icon Utils
        
        private static Stream GetIconStream(string iconName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyName = assembly.GetName().Name;
            return assembly.GetManifestResourceStream($"{assemblyName}.Resources.{iconName}");
        }

        #endregion Icon Utils
    }
}