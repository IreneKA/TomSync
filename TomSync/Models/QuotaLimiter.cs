namespace TomSync.Models
{
    public class QuotaLimiter
    {
        public long? QuotaAvailableBytes { get; set; }
        public long? QuotaUsedBytes { get; set; }
        public long? QuotaBytes
        {
            get
            {
                long quotaUsedBytes;
                long quotaAvailableBytes;

                if (QuotaUsedBytes.HasValue)
                    quotaUsedBytes = QuotaUsedBytes.Value;
                else
                    return null;

                if (QuotaAvailableBytes.HasValue)
                    quotaAvailableBytes = QuotaAvailableBytes.Value;
                else
                    return null;

                if (quotaAvailableBytes < 0)
                    quotaAvailableBytes = 0;

                return quotaUsedBytes + quotaAvailableBytes;
            }
        }
        public bool IsLimited
        {
            get
            {
                if (QuotaAvailableBytes.HasValue)
                {
                    if (QuotaAvailableBytes.Value < 0)
                        return false;
                    else
                        return true;
                }
                else
                    return false;
            }
        }

        public QuotaLimiter()
        {
            QuotaAvailableBytes = null;
            QuotaUsedBytes = null;
        }
    }
}
