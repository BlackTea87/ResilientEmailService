namespace ResilientEmailService.Services.CircuitBreakers
{
    public class CircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _resetTimeout;
        private int _failureCount;
        private DateTime _lastFailureTime;
        private bool _isCircuitOpen;

        public CircuitBreaker(int failureThreshold, TimeSpan resetTimeout)
        {
            _failureThreshold = failureThreshold;
            _resetTimeout = resetTimeout;
        }

        public bool IsCircuitOpen()
        {
            if (_isCircuitOpen && DateTime.UtcNow - _lastFailureTime > _resetTimeout)
            {
                _isCircuitOpen = false;
                _failureCount = 0;
                return false;
            }
            return _isCircuitOpen;
        }

        public void RecordFailure()
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _isCircuitOpen = true;
            }
        }

        public void Reset()
        {
            _failureCount = 0;
            _isCircuitOpen = false;
        }
    }
}
