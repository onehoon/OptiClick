using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OptiClick.Wpf.Collections;

public sealed class BatchedObservableCollection<T> : ObservableCollection<T>
{
    public BatchedObservableCollection()
    {
    }

    public BatchedObservableCollection(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var snapshot = items as IReadOnlyList<T> ?? items.ToArray();
        Items.Clear();
        foreach (var item in snapshot)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
