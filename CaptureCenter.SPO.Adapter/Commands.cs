using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace CaptureCenter.SPO
{
    public class SPOExportConnectorCommands
    {
        public static RoutedUICommand TestConnection { get { return testConnection; } }
        private static RoutedUICommand testConnection = new RoutedUICommand(
            "Test connection", "testConnection", typeof(SPOExportConnectorCommands));

        public static RoutedUICommand Version { get { return version; } }
        private static RoutedUICommand version = new RoutedUICommand(
            "Show version", "showVersion", typeof(SPOExportConnectorCommands),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.V, ModifierKeys.Alt)
        }));

        public static RoutedUICommand Login { get { return login; } }
        private static RoutedUICommand login = new RoutedUICommand(
            "Login", "Login to SharePoint", typeof(SPOExportConnectorCommands),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.L, ModifierKeys.Control)
        }));

        public static RoutedUICommand Filter { get { return filter; } }
        private static RoutedUICommand filter = new RoutedUICommand(
            "Filter", "Lauch filter dialog", typeof(SPOExportConnectorCommands),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.F, ModifierKeys.Control)
        }));
    }
}
