using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MemeticThreatClient.Data;
using MemeticThreatClient.Views;
using MemeticThreatClient.ViewModels;

namespace MemeticThreatClient
{
    public partial class MainWindow : Window, IMainWindowCodeBehind
    {
        public bool Authorized { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            Authorized = false;

            LoadView(ViewType.Main);
        }

        public void LoadView(ViewType viewType)
        {
            switch (viewType)
            {
                case ViewType.Main:
                    MainView mainView = new MainView();
                    MainViewModel mainViewModel = new MainViewModel(this, Authorized);
                    mainView.DataContext = mainViewModel;
                    OutputView.Content = mainView;
                    break;
                case ViewType.Auth:
                    AuthView authView = new AuthView();
                    AuthViewModel authViewModel = new AuthViewModel(this);
                    authView.DataContext = authViewModel;
                    OutputView.Content = authView;
                    break;
                case ViewType.Reg:
                    RegView regView = new RegView();
                    RegViewModel regViewModel = new RegViewModel(this);
                    regView.DataContext = regViewModel;
                    OutputView.Content = regView;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
