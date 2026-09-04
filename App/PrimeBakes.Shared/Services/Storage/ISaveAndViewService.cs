namespace PrimeBakes.Shared.Services.Storage;

public interface ISaveAndViewService
{
    public Task<string> SaveAndView(string fileName, MemoryStream stream);
}
