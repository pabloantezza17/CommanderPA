namespace Framework.Entities
{
    public class ValueOf<T>
    {
        public ValueOf() { }

        public ValueOf(T value)
        {
            this.Value = value;
        }

        public T Value { get; set; }
    }
}