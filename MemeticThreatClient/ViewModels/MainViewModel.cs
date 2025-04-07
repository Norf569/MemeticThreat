using MemeticThreatClient.Data;
using MemeticThreatClient.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;

namespace MemeticThreatClient.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged, ISelectedItemObserver, IFileUpdateObserver
    {
        private IMainWindowCodeBehind _codeBehind;
        private Client client;
        private IDataInstance _SelectedFile;

        public MainViewModel(IMainWindowCodeBehind _codeBehind, bool authorized)
        {
            this._codeBehind = _codeBehind;
            Client.SetFileUpdateObserver = this;
            client = Client.GetInstance();

            NodeWrapper._observer = this;

            if (authorized)
                UnauthorizedButton_Visibility = Visibility.Collapsed;
            else
                UnauthorizedButton_Visibility = Visibility.Visible;

            UploadingPBVisibility = Visibility.Collapsed;
        }

        private RelayCommand _AuthCommand;
        public RelayCommand AuthCommand
        {
            get
            {
                return _AuthCommand ?? (_AuthCommand = new RelayCommand(obj =>
                {
                    _codeBehind.LoadView(ViewType.Auth);
                }));
            }
        }

        private RelayCommand _RegCommand;
        public RelayCommand RegCommand
        {
            get
            {
                return _RegCommand ?? (_RegCommand = new RelayCommand(obj =>
                {
                    SelectedItem = null;
                    _codeBehind.LoadView(ViewType.Reg);
                }));
            }
        }
        private RelayCommand _LogOutCommand;
        public RelayCommand LogOutCommand
        {
            get
            {
                return _LogOutCommand ?? (_LogOutCommand = new RelayCommand(obj =>
                {
                    client.Logout();
                    SelectedItem = null;
                    UnauthorizedButton_Visibility = Visibility.Visible;
                }));
            }
        }
        private RelayCommand _DeleteUserCommand;
        public RelayCommand DeleteUserCommand
        {
            get
            {
                return _DeleteUserCommand ?? (_DeleteUserCommand = new RelayCommand(obj =>
                {
                    if (MessageBox.Show("Вы уверены, что хотите удалить пользователя?",
                        "Удаление пользователя",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            client.DeleteUser();
                            SelectedItem = null;
                            UnauthorizedButton_Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Не получилось удалить пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }));
            }
        }
        private RelayCommand _UpdateButtonCommand;
        public RelayCommand UpdateButtonCommand
        {
            get
            {
                return _UpdateButtonCommand ?? (_UpdateButtonCommand = new RelayCommand(obj =>
                {
                    UpdateFiles();
                }));
            }
        }
        private RelayCommand _DownloadButtonCommand;
        public RelayCommand DownloadButtonCommand
        {
            get
            {
                return _DownloadButtonCommand ?? (_DownloadButtonCommand = new RelayCommand(obj =>
                {
                    try
                    {
                        if (_SelectedFile == null)
                            MessageBox.Show("Файл не выбран.");
                        else if (_SelectedFile is FileModel)
                        {
                            client.DownloadFile(((FileModel)_SelectedFile).Path + _SelectedFile.Name);
                        }
                        else
                            MessageBox.Show("Пока что не можем скачать папку :)");
                    }
                    catch (UnauthorizedAccessException) 
                    {
                        MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    } catch (Exception)
                    {
                        MessageBox.Show("Не удалось скачать файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
        }
        private RelayCommand _UploadButtonCommand;
        public RelayCommand UploadButtonCommand
        {
            get
            {
                return _UploadButtonCommand ?? (_UploadButtonCommand = new RelayCommand(obj =>
                {
                    try
                    {
                        if (client.User.Jwt == null)
                        {
                            MessageBox.Show("Пользователь не авторизован.");
                            return;
                        }

                        OpenFileDialog dlg = new OpenFileDialog();

                        Nullable<bool> result = dlg.ShowDialog();
                        if (result == true)
                        {
                            client.UploadFile(dlg.FileName);
                            OnPropertyChanged("FileTreeCollection");
                        }
                    } catch (FileFormatException)
                    {
                        MessageBox.Show("Размер файла превышает дофига байт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    } catch (FileLoadException)
                    {
                        MessageBox.Show("Недостаточно места", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    } catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    } catch (Exception ex) { Trace.WriteLine(ex); };
                }));
            }
        }
        private RelayCommand _DeleteButtonCommand;
        public RelayCommand DeleteButtonCommand
        {
            get
            {
                return _DeleteButtonCommand ?? (_DeleteButtonCommand = new RelayCommand(obj =>
                {
                    try
                    {
                        if (_SelectedFile == null)
                            MessageBox.Show("Файл не выбран.");
                        else if (_SelectedFile is FileModel)
                        {
                            client.DeleteFile(((FileModel)_SelectedFile).Id);
                            OnPropertyChanged("FileTreeCollection");
                        }
                        else
                            MessageBox.Show("Пока что не можем удалить папку :)");
                    } 
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception) 
                    {
                        MessageBox.Show("Не удалось удалить файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
        }

        public void UpdateFiles()
        {
            try
            {
                if (client.User.Jwt == null)
                {
                    MessageBox.Show("Пользователь не авторизован.");
                    return;
                }

                client.FileTree = null;
                SelectedItem = null;
                OnPropertyChanged("FileTreeCollection");

                client.GetFiles();
                OnPropertyChanged("FileTreeCollection");
                OnPropertyChanged("StorageInfo");
                //if (client.FileTree == null || client.FileTree.Children == null || client.FileTree.Children.Count > 0)
                //    SelectedItem = null;
                //SelectedItem = null;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show("Не получилось получить список файлов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public object SelectedItem
        {
            get => _SelectedFile;
            set
            {
                if (value == null) 
                    _SelectedFile = null;
                else if (value is IDataInstance)
                    _SelectedFile = value as IDataInstance;
                OnSelectedItemChanged();

                //else
                //    throw new Exception("Value isn't IDataInstace");
            }
        }
        public void UploadedFilesUpdate(int uploadedFilesCount, int filesCount)
        {
            if (filesCount != FilePartsCount)
            {
                FilePartsCount = filesCount;
                OnPropertyChanged("FilePartsCount");
            }
            UploadedFilesCount = uploadedFilesCount;
            OnPropertyChanged("UploadedFilesCount");
        }
        public void UploadedFilesUpdate(bool uploadingStarted)
        {
            if (UploadingPBVisibility == Visibility.Visible && uploadingStarted == false)
                UpdateFiles();

            UploadingPBVisibility = uploadingStarted ? Visibility.Visible : Visibility.Collapsed;
            OnPropertyChanged("UploadingPBVisibility");
        }

        public string UserName { get => client.User.Name ?? "Неавторизован"; }
        public string StorageInfo
        {
            get
            {
                if (client.User.StorageSpace != 0) {
                    double usedSpace = Math.Round(client.User.TotalFileSize / 1024.0 / 1024.0 / 1024.0, 2);
                    double storageSpace = Math.Round(client.User.StorageSpace / 1024.0 / 1024.0 / 1024.0);
                    return "Storage usage: " + usedSpace + "/" + storageSpace + " ГБ";
                }
                else
                    return "";
            }
        }
        public string FileName { get => _SelectedFile != null ? $"Имя фалйа: {_SelectedFile.Name}" : ""; }
        public string FilePath 
        { 
            get
            {
                if (_SelectedFile != null)
                {
                    if (_SelectedFile is FileModel)
                    {
                        return $"Путь: {((FileModel)_SelectedFile).Path}";
                    }
                }
                return "";
            }
        }
        public string FileSize
        {
            get
            {
                if (_SelectedFile != null)
                {
                    if (_SelectedFile is FileModel)
                    {
                        return $"Размер файла: {Math.Round(((FileModel)_SelectedFile).FileSize / 1024.0 / 1024.0, 2)} МБ";
                    }
                }
                return "";
            }
        }
        public string FileUser { get => ""; }
        public Visibility UploadingPBVisibility { set; get; }
        public int FilePartsCount { get; set; }
        public int UploadedFilesCount { get; set; }
        public ObservableCollection<Node<IDataInstance>> FileTreeCollection 
        { 
            get => (client.FileTree != null) ? client.FileTree.Children : null; 
        }

        private Visibility _Visibility;
        public Visibility UnauthorizedButton_Visibility 
        { 
            get => _Visibility; 
            set 
            {
                _Visibility = value;
                OnPropertyChanged("UserName");
                OnPropertyChanged("StorageInfo");
                OnPropertyChanged("FileTreeCollection");
                OnPropertyChanged("FileName");
                OnPropertyChanged("UnauthorizedButton_Visibility");
                OnPropertyChanged("AuthorizedButton_Visibility");
            } 
        }
        public Visibility AuthorizedButton_Visibility 
        { 
            get
            {
                if (UnauthorizedButton_Visibility == Visibility.Collapsed) 
                    return Visibility.Visible;
                else 
                    return Visibility.Collapsed;
            }
        }

        private void OnSelectedItemChanged()
        {
            OnPropertyChanged("FileName");
            OnPropertyChanged("FilePath");
            OnPropertyChanged("FileSize");
            OnPropertyChanged("FileUser");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
