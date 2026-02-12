using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DeleterServices.Services;

/// <summary>
/// Abstraction to delete difference type messages. Because need absract data
/// </summary>
public interface IMessageToDelete{}

/// <summary>
/// Use for delete message from system/
/// For another services need multiple method to delete and difference data to delete
/// </summary>
/// <typeparam name="T">Need set type message who will be deleted</typeparam>
public interface IDeleterService<T> where T : struct, IMessageToDelete
{
    /// <summary>
    /// Method to delete data
    /// </summary>
    /// <param name="dataArray">List to deletes</param>
    /// <param name="secondsToDelete">Time to delete</param>
    /// <returns>Task to throwing exceptions</returns>
    Task Delete(List<T> dataArray, int secondsToDelete);
}

/// <summary>
/// Class to collect message to deleting and sending to DeleterService
/// </summary>
/// <typeparam name="T">Type data to collect</typeparam>
public sealed class MessageDeleterCollector<T>
where T : struct, IMessageToDelete
{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DateTime GetNextTimeToBatch() => DateTime.UtcNow.AddSeconds(_countSecondsToDelete);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<T> CreateNewList() => new List<T>(50);
    
    
    private IDeleterService<T> _deleterService;
    
    private List<T> _listToDelete;
    public int GetCount() => _listToDelete.Count;
    
    private DateTime _neededTimeToBatch = DateTime.UtcNow;
    
    private readonly int _countSecondsToDelete = 0;
    private readonly int _countSecondsToBatch = 0;


    public MessageDeleterCollector(IDeleterService<T> deleterService
        , int countSecondsToDelete
        , int countSecondsToBatchDeleteMessage)
    {
        _listToDelete =  CreateNewList();
        _deleterService =  deleterService;
        _countSecondsToDelete =  countSecondsToDelete;
        _countSecondsToBatch = countSecondsToBatchDeleteMessage;
        _neededTimeToBatch = GetNextTimeToBatch();
    }
    
    /// <summary>
    /// Lock
    /// </summary>
    private readonly object _addMethodLock = new object();
    
    /// <summary>
    /// Add data to delete
    /// </summary>
    /// <param name="messageDeleter"></param>
    public async Task Add(T messageDeleter)
    {
        List<T> batchToSend = null;
    
        lock (_addMethodLock) // Используем ОДИН из lock'ов для всего
        {
            _listToDelete.Add(messageDeleter);
        
            // Простая логика: если список достиг размера - отправляем
            if (_neededTimeToBatch < DateTime.UtcNow)
            {
                _neededTimeToBatch = GetNextTimeToBatch();
                if (_listToDelete.Count != 0)
                {
                    batchToSend = _listToDelete;
                    _listToDelete = CreateNewList();
                }

            }
        }

        if (batchToSend != null)
        {
            await _deleterService.Delete(batchToSend, _countSecondsToDelete);
        }

    }
}

/// <summary>
/// Mock data
/// </summary>
/// <param name="data"></param>
public record struct MockDataToDelete : IMessageToDelete
{
    private int _data = 0;

    public MockDataToDelete(int data)
    {
        _data = data;
    }
    public int Data => _data;
}

/// <summary>
/// Mock deleter service
/// Emulate delete telegram data
/// </summary>
public class MockDeleterService : IDeleterService<MockDataToDelete>
{
    private readonly MockDataRepository _mockDataRepository;

    public MockDeleterService(MockDataRepository mockDataRepository)
    {
        _mockDataRepository = mockDataRepository;
    }

    public async Task Delete(List<MockDataToDelete> dataArray, int secondsToDelete)
    {
        await Task.Delay(1000);
        _mockDataRepository.RemoveRangeMockDataToDelete(dataArray.AsEnumerable());
        Console.WriteLine("Swag deleted");
    }
}

public sealed class Result<T, E>
where E: Exception
{
    public T Data { get; set; }
    
    public Result(T val, E error)
    {
        
    }
}

public struct MockDataRepository
{
    private List<MockDataToDelete> _data = new List<MockDataToDelete>();
    private readonly object _dataLock = new object();

    public MockDataRepository()
    {
    }

    public bool Add(MockDataToDelete mockDataToDelete)
    {
        lock (_dataLock)
        {
            if (!_data.Contains(mockDataToDelete))
            {
                _data.Add(mockDataToDelete);
                return true;
            }
            return false;
        }
    }
    //Dont reload function to dont create virtual table method
    public void RemoveSingle(MockDataToDelete mockDataToDelete)
    {
        lock (_dataLock)
        {
            
            _data.Remove(mockDataToDelete);
        }
    }
    
    public void RemoveRangeInt(IEnumerable<int> mockDataToDelete)
    {
        lock (_dataLock)
        {
            foreach (var item in mockDataToDelete)
            {
                _data.RemoveAll((x) => x.Data == item);
            }
        }
    }
    
    public void RemoveRangeMockDataToDelete(IEnumerable<MockDataToDelete> mockDataToDelete)
    {
        try
        {
            lock (_dataLock)
            {
                foreach (var item in mockDataToDelete)
                {
                    _data.Remove(item);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }

    public int? TryGet(int data)
    {
        lock (_dataLock)
        {
            var slice  = _data.Where(x => x.Data == data);
            if (slice.Count() == 0)
            {
                return null;
            }

            return slice.First().Data;
        }
    }


}
