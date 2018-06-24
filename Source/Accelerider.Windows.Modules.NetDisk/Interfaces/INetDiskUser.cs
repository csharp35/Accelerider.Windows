﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Accelerider.Windows.Infrastructure;
using Accelerider.Windows.Infrastructure.Interfaces;
using Accelerider.Windows.Modules.NetDisk.Enumerations;
using Accelerider.Windows.Modules.NetDisk.Models;

namespace Accelerider.Windows.Modules.NetDisk.Interfaces
{
    public interface INetDiskUser
    {
        // The user info ----------------------------------------------------------------------

        string Username { get; }

        DataSize TotalCapacity { get; }

        DataSize UsedCapacity { get; }

        Task RefreshUserInfoAsync();

        // Gets files info from server --------------------------------------------------------

        Task<ILazyTreeNode<INetDiskFile>> GetFileRootAsync();

        Task<IReadOnlyCollection<FileCategory>> GetAvailableCategoriesAsync();

        Task<IList<T>> GetFilesAsync<T>(FileCategory category) where T : IFile;

        // Transfer operations ----------------------------------------------------------------

        Task DownloadAsync(ILazyTreeNode<INetDiskFile> from, FileLocator to, Action<TransferItem> callback);

        // It will be throw an exception if the to.FileType is not folder type.
        Task UploadAsync(FileLocator from, INetDiskFile to, Action<TransferItem> callback);
    }
}
