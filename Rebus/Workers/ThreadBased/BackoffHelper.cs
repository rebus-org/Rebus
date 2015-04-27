using System.Threading.Tasks;

namespace Rebus.Workers.ThreadBased
{
    public class BackoffHelper
    {
        public async Task Wait()
        {
            await Task.Delay(200);
        }

        public void Reset()
        {
            
        }
    }
}