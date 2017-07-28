using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace UniversalAnnotator
{

    class EntityType
    {
        public EntityType(string name, Color color)
        {
            Name = name;
            Color = color;
        }
        public String Name { get; set; }
        public Color Color { get; set; }
    }


    /// <summary>
    ///  The list of entities defined.
    /// </summary>
    class EntityCollection : Dictionary<string, EntityType>
    {
        public void AddEntity(EntityType entitytype)
        {
            if (this.ContainsKey(entitytype.Name))
            {
                this.Remove(entitytype.Name);
            }

            this.Add(entitytype.Name, entitytype);
        }

        /*
        public Color GetColor(String entityName)
        {
            EntityType value;
            this.TryGetValue(entityName, out value);
            return value.Color;
        }*/
    }
}
