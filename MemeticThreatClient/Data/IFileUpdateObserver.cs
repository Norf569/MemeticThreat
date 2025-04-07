using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeticThreatClient.Data
{
    internal interface IFileUpdateObserver
    {
        void UploadedFilesUpdate(int uploadedFilesCount, int filesCount);
        void UploadedFilesUpdate(bool started);
        void UpdateFiles();
    }
}
