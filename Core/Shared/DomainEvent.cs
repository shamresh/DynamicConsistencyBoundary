namespace Core.Domain.Shared
{
    public abstract class DomainEvent
    {
        public long Position { get; }

        protected DomainEvent(long position)
        {
            Position = position;
        }
    }
} 