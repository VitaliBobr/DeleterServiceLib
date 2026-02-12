namespace DeleterServices.Test;
using DeleterServices.Services;
public class DeleterTest
{
    [Fact]
    public void TestMockDataCreation()
    {
        List<MockDataToDelete> list = new List<MockDataToDelete>(10);
        
        foreach (var item in Enumerable.Range(1, 10))
        {
            list.Add(new MockDataToDelete(item));
        }

        int i = 1;

        Assert.True(list.All((x) => x.Data == i++));
    } 
    
    [Fact]
    public async Task TestMockDataAddingToCollector()
    {
        //Initialize
        MockDataRepository repos = new MockDataRepository();
        MockDeleterService deleter = new MockDeleterService(repos);
        MessageDeleterCollector<MockDataToDelete> dataDeleter =
            new MessageDeleterCollector<MockDataToDelete>(deleter, 60, 60);
        

        foreach (var item in Enumerable.Range(1, 10))
        {
            repos.Add(new MockDataToDelete(item));
        }

        foreach (var item in Enumerable.Range(1, 10))
        {
            Assert.True(repos.TryGet(item).GetValueOrDefault(-1) == item); 
        }
    } 
    
    [Fact]
    public async Task TestMockDataToBatchAndDelete()
    {
        int secondToDelete = 3;
        int  secondToBatch = 3;
        //Initialize
        MockDataRepository repos = new MockDataRepository();
        MockDeleterService deleter = new MockDeleterService(repos);
        MessageDeleterCollector<MockDataToDelete> dataDeleter =
            new MessageDeleterCollector<MockDataToDelete>(deleter, secondToDelete, secondToBatch);
        

        foreach (var item in Enumerable.Range(1, 10))
        {
            repos.Add(new MockDataToDelete(item));
        }

        foreach (var item in Enumerable.Range(1, 9))
        {
            dataDeleter.Add(new  MockDataToDelete(item));
        }
        
        await Task.Delay((secondToBatch+1)*1000);
        
        dataDeleter.Add(new MockDataToDelete(10));

        
        await Task.Delay(100);
        
        Assert.Equal(0,dataDeleter.GetCount());

        foreach (var item in Enumerable.Range(1, 10))
        {
            Assert.True(repos.TryGet(item).HasValue);
        }
        
        await Task.Delay((secondToDelete+10) * 1000);
        
        foreach (var item in Enumerable.Range(1, 10))
        {
            var val = repos.TryGet(item);
            var val2 = val.HasValue;
            Assert.False(val2);
        }
    } 
    
    //[Fact]
    public async Task IntegrationGitLabTest()
    {
        Assert.False(true);
    }
}