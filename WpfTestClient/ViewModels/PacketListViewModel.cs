using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfTestClient.Models;

namespace WpfTestClient.ViewModels;

public sealed partial class PacketListViewModel : ObservableObject
{
    public IReadOnlyList<PacketItem> AvailablePackets { get; } = PacketItem.All;

    [ObservableProperty]
    private PacketItem? _selectedAvailable;

    public ObservableCollection<PacketItem> ScenarioQueue { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private PacketItem? _selectedQueue;

    public bool IsConfirmed { get; private set; }

    [RelayCommand]
    private void Add()
    {
        if (SelectedAvailable is null) return;
        ScenarioQueue.Add(SelectedAvailable);
    }

    private bool CanRemove() => SelectedQueue is not null;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedQueue is null) return;
        ScenarioQueue.Remove(SelectedQueue);
        SelectedQueue = null;
    }

    private bool CanMoveUp() => SelectedQueue is not null && ScenarioQueue.IndexOf(SelectedQueue) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedQueue is null) return;
        int index = ScenarioQueue.IndexOf(SelectedQueue);
        if (index <= 0) return;
        ScenarioQueue.Move(index, index - 1);
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private bool CanMoveDown() => SelectedQueue is not null && ScenarioQueue.IndexOf(SelectedQueue) < ScenarioQueue.Count - 1;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedQueue is null) return;
        int index = ScenarioQueue.IndexOf(SelectedQueue);
        if (index >= ScenarioQueue.Count - 1) return;
        ScenarioQueue.Move(index, index + 1);
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Confirm(Window window)
    {
        IsConfirmed = true;
        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        IsConfirmed = false;
        window.DialogResult = false;
        window.Close();
    }
}
