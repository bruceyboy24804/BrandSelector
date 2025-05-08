using Unity.Entities;

namespace BrandSelector.Domain
{
    public class BrandInfo
    {
        private string m_Name;
        private Entity m_Entity;

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public Entity Entity
        {
            get { return m_Entity; }
            set { m_Entity = value; }
        }

        public BrandInfo()
        {
        }

        public BrandInfo(string name, Entity entity)
        {
            m_Name = name;
            m_Entity = entity;
        }
    }
}