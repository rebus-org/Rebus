namespace Rebus2.Config
{
    public class OptionsConfigurer
    {
        readonly Options _options;

        public OptionsConfigurer(Options options)
        {
            _options = options;
        }

        public OptionsConfigurer SetNumberOfWorkers(int numberOfWorkers)
        {
            _options.NumberOfWorkers = numberOfWorkers;
            return this;
        }
    }
}