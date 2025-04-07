using MemeticThreatClient.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemeticThreatClient.ViewModels
{
    internal class RegViewModel
    {
        private IMainWindowCodeBehind _codeBehind;
        private Client client;
        public RegViewModel(IMainWindowCodeBehind _codeBehind)
        {
            this._codeBehind = _codeBehind;
            client = Client.GetInstance();
        }

        public string UserNameText { get; set; }
        public string EmailText { get; set; }
        public string Password1Text { get; set; }
        public string Password2Text { get; set; }

        private RelayCommand _RegCommand;
        public RelayCommand RegCommand
        {
            get
            {
                return _RegCommand ?? (_RegCommand = new RelayCommand(obj =>
                {
                    if (UserNameText == null || EmailText == null || Password1Text == null || Password2Text == null)
                        MessageBox.Show("Все поля должны быть заполнены.", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                    else if (Password1Text != Password2Text)
                        MessageBox.Show("Пароли не совпадают.", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                    {
                        try
                        {
                            //проверка на email

                            client.Register(UserNameText, EmailText, Password1Text);
                            _codeBehind.Authorized = true;
                            _codeBehind.LoadView(ViewType.Main);
                            client.GetFiles();
                        } catch (AuthenticationException)
                        {
                            MessageBox.Show("Ошибка аунтификации.", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                        } catch (Exception ex)
                        {
                            if (ex.Message == "Auth exception")
                                MessageBox.Show("Ошибка регистрации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            else
                                MessageBox.Show("Сервер не отвечает.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
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
