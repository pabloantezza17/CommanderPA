using System;

namespace Framework.Entities
{
    public class Entity
    {
        public Entity()
        {
        }

        public Entity(Int64 id, String description = null)
        {
            this.Id = id;
            this.Description = description;
        }

        public Int64 Id { get; set; }

        public String Description { get; set; }

        public override String ToString()
        {
            return this.Description;
        }
    }
}