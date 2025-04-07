using MemeticThreatClient.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemeticThreatClient.ViewModels
{
    internal class AuthViewModel
    {
        //public event PropertyChangedEventHandler PropertyChanged;
        private IMainWindowCodeBehind _codeBehind;
        private Client client;
        public AuthViewModel(IMainWindowCodeBehind _codeBehind) 
        {
            this._codeBehind = _codeBehind;
            _codeBehind.Authorized = false;
            client = Client.GetInstance();
        }

        private string _UserNameText;
        public string UserNameText
        {
            get { return _UserNameText; }
            set
            {
                _UserNameText = value;
                //PropertyChanged(this, new PropertyChangedEventArgs(nameof(UserNameText)));
            }
        }
        private string _PasswordText;
        public string PasswordText
        {
            get { return _PasswordText; }
            set
            {
                _PasswordText = value;
                //PropertyChanged(this, new PropertyChangedEventArgs(nameof(UserNameText)));
            }
        }
        private RelayCommand _LoginCommand;
        public RelayCommand LoginCommand
        {
            get
            {
                return _LoginCommand ?? (_LoginCommand = new RelayCommand(obj =>
                {
                    try
                    {
                        client.Login(_UserNameText, _PasswordText);
                        _codeBehind.Authorized = true;
                        _codeBehind.LoadView(ViewType.Main);
                        client.GetFiles();
                    }
                    catch (AuthenticationException)
                    {
                        MessageBox.Show("Пользователь не найден.", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Auth exception")
                            MessageBox.Show("Ошибка аунтификации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        else
                            MessageBox.Show("Сервер не отвечает.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
        }
        private RelayCommand _CancelCommand;
        public RelayCommand CancelCommand
        {
            get
            {
                return _CancelCommand ?? (_CancelCommand = new RelayCommand(obj =>
                {
                    _codeBehind.LoadView(ViewType.Main);
                }));
            }
        }
    }
}
