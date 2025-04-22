using System;
using BrandSelector.Extensions;
using Colossal.Entities;
using Unity.Entities;
using Game.Companies;
using Game.UI.InGame;
using Game.Buildings;
using Game.Prefabs;
using Game.Common;
using Game.Economy;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game;
using System.Linq;
using Colossal.Json;

namespace BrandSelector.Systems
{
    public partial class BrandListSection : ExtendedInfoSectionBase
    {
        private Entity m_CompanyEntity;
        private BrandData[] m_Brands = Array.Empty<BrandData>();
        private ValueBindingHelper<BrandData[]> m_BrandsBinding;
        private PrefabSystem m_PrefabSystem;

        protected override string group => "BrandListSection";

        private bool Visible()
        {
            return CompanyUIUtils.HasCompany(EntityManager, selectedEntity, selectedPrefab, out m_CompanyEntity);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_PrefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            
            // Create the binding here in OnCreate
            m_BrandsBinding = CreateBinding("brands", Array.Empty<BrandData>());
        }

        protected override void Reset()
        {
            m_CompanyEntity = Entity.Null;
            m_Brands = Array.Empty<BrandData>();

            if (m_BrandsBinding != null)
            {
                // Directly assign the empty array
                m_BrandsBinding.Value = m_Brands;
            }
        }

        protected override void OnUpdate()
        {
            visible = Visible();

            if (!visible || selectedPrefab == Entity.Null || !EntityManager.Exists(selectedPrefab))
            {
                if (m_Brands.Length > 0)
                {
                    m_Brands = Array.Empty<BrandData>();
                    m_BrandsBinding.Value = m_Brands;
                }
                base.OnUpdate();
                return;
            }

            var tempBrandsList = new List<BrandData>();

            // First path: through Renter buffer
            if (EntityManager.TryGetBuffer<Game.Buildings.Renter>(selectedEntity, true, out DynamicBuffer<Game.Buildings.Renter> renterBuffer) && 
                renterBuffer.Length > 0)
            {
                // For each renter in the buffer
                foreach (var renter in renterBuffer)
                {
                    Entity renterEntity = renter.m_Renter;
                    
                    // Check if renter entity exists and has a PrefabRef component
                    if (EntityManager.Exists(renterEntity) && 
                        EntityManager.TryGetComponent<Game.Prefabs.PrefabRef>(renterEntity, out var prefabRef))
                    {
                        Entity prefabEntity = prefabRef.m_Prefab;
                        
                        // Check if prefab entity exists and has CompanyBrandElement buffer
                        if (EntityManager.Exists(prefabEntity) &&
                            EntityManager.TryGetBuffer<CompanyBrandElement>(prefabEntity, true, out DynamicBuffer<CompanyBrandElement> brandElements))
                        {
                            ProcessBrandElements(brandElements, tempBrandsList);
                        }
                    }
                }
            }
            // Alternative path through direct PrefabRef
            else if (EntityManager.TryGetComponent<Game.Prefabs.PrefabRef>(selectedEntity, out var prefabRef))
            {
                Entity prefabEntity = prefabRef.m_Prefab;
                
                if (EntityManager.Exists(prefabEntity) &&
                    EntityManager.TryGetBuffer<CompanyBrandElement>(prefabEntity, true, out DynamicBuffer<CompanyBrandElement> brandElements))
                {
                    ProcessBrandElements(brandElements, tempBrandsList);
                }
            }

            m_Brands = tempBrandsList.ToArray();
            m_BrandsBinding.Value = m_Brands;

            base.OnUpdate();
        }

// Helper method to process brand elements
            private void ProcessBrandElements(DynamicBuffer<CompanyBrandElement> brandElements, List<BrandData> brandsList)
            {
                foreach (var brandElement in brandElements)
                {
                    Entity brandEntity = brandElement.m_Brand;
                    string brandName = "Unknown Brand";

                    if (m_PrefabSystem != null && m_PrefabSystem.TryGetPrefab(brandEntity, out PrefabBase brandPrefab))
                    {
                        brandName = brandPrefab.name;
                    }

                    brandsList.Add(new BrandData
                    {
                        id = brandEntity.Index.ToString(),
                        name = brandName ?? "Unnamed Brand"
                    });
                }

                if (brandElements.Length > 0)
                {
                    tooltipKeys.Add("CompanyBrands");
                }
            }

        protected override void OnProcess()
        {
            // Empty method as required by base class
        }
        
        public override void OnWriteProperties(IJsonWriter writer)
        {
            
        }
    }

    public class BrandData
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}