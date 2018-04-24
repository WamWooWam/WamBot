using System.Threading.Tasks;

namespace WamBotRewrite
{
    public interface IFactory<T>
    {
        T Create(object key);

        Task<T> CreateAsync(object key);
    }
}