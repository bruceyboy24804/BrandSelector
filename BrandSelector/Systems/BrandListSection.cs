using System;
using System.Runtime.CompilerServices;
using BrandSelector.Extensions;
using Colossal.Entities;
using Colossal.UI.Binding;
using Unity.Entities;
using Unity.Collections;
using Game.Companies;
using Game.UI.InGame;
using Game.Buildings;
using Game.Prefabs;
using Game.Common;
using Game.UI;  // Add this import for NameSystem
using UnityEngine.Scripting;

namespace BrandSelector.Systems
{
    // Define a serializable struct that holds both name and entity
    [Serializable]
    public struct BrandInfo
    {
        public string name;
        public Entity entity;
        
        public BrandInfo(string name, Entity entity)
        {
            this.name = name;
            this.entity = entity;
        }
    }

    public partial class BrandListSection : ExtendedInfoSectionBase
    {
        protected override string group => "BrandListSection";
        private Entity m_CompanyEntity;
        private PrefabSystem m_PrefabSystem;
        private NameSystem m_NameSystem;  // Add NameSystem reference
        private NativeList<Entity> m_AvailableBrands;
        private Entity m_SelectedBrand;
        private ValueBindingHelper<BrandInfo[]> m_AvailableBrandsBinding;
        private ValueBindingHelper<BrandInfo> m_SelectedBrandBinding;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_PrefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            m_NameSystem = World.GetExistingSystemManaged<NameSystem>();  // Get NameSystem
            m_AvailableBrands = new NativeList<Entity>(4, Allocator.Persistent);
            
            // Create bindings
            m_AvailableBrandsBinding = CreateBinding("availableBrands", Array.Empty<BrandInfo>());
            m_SelectedBrandBinding = CreateBinding("selectedBrand", "selectBrand", new BrandInfo("", Entity.Null), SelectBrand);
        }
        
        private bool Visible()
        {
	        return CompanyUIUtils.HasCompany(base.EntityManager, selectedEntity, selectedPrefab, out m_CompanyEntity);
        }

        [Preserve]
        protected override void OnUpdate()
        {
	        base.visible = Visible();
        }
        
        private void SelectBrand(BrandInfo brandInfo)
        {
            m_SelectedBrand = brandInfo.entity;
            m_SelectedBrandBinding.Value = brandInfo;
            
            // Update the renter to the selected brand
            if (selectedEntity != Entity.Null && EntityManager.HasComponent<Renter>(selectedEntity))
            {
                var entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
                
                // Get the renter buffer
                var renterBuffer = EntityManager.GetBuffer<Renter>(selectedEntity);
                if (renterBuffer.Length > 0)
                {
                    // Update the first renter to the selected brand
                    var renter = renterBuffer[0];
                    renter.m_Renter = brandInfo.entity;
                    renterBuffer[0] = renter;
                }
                else
                {
                    // Add new renter if none exists
                    renterBuffer.Add(new Renter { m_Renter = brandInfo.entity });
                }
            }
            
            m_InfoUISystem.RequestUpdate();
        }

        [Preserve]
        protected override void OnDestroy()
        {
            m_AvailableBrands.Dispose();
            base.OnDestroy();
        }

        protected override void Reset()
        {
            m_CompanyEntity = Entity.Null;
            m_SelectedBrand = Entity.Null;
            m_SelectedBrandBinding.Value = new BrandInfo("", Entity.Null);
            m_AvailableBrandsBinding.Value = Array.Empty<BrandInfo>();
            m_AvailableBrands.Clear();
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            // The bindings will handle writing the properties automatically
        }

        protected override void OnProcess()
        {
            if (!visible || selectedPrefab == Entity.Null || !EntityManager.Exists(selectedPrefab))
            {
                Reset();
                return;
            }

            m_AvailableBrands.Clear();
            var brandInfos = new System.Collections.Generic.List<BrandInfo>();

            // Get current renter to identify selected brand
            if (EntityManager.HasComponent<Renter>(selectedEntity))
            {
                var renterBuffer = EntityManager.GetBuffer<Renter>(selectedEntity);
                if (renterBuffer.Length > 0)
                {
                    var renterEntity = renterBuffer[0].m_Renter;
                    if (EntityManager.HasComponent<PrefabRef>(renterEntity))
                    {
                        var prefabRef = EntityManager.GetComponentData<PrefabRef>(renterEntity);
                        if (EntityManager.HasBuffer<CompanyBrandElement>(prefabRef.m_Prefab))
                        {
                            var brandElements = EntityManager.GetBuffer<CompanyBrandElement>(prefabRef.m_Prefab);
                            foreach (var brandElement in brandElements)
                            {
                                var brandEntity = brandElement.m_Brand;
                                m_AvailableBrands.Add(brandEntity);
                                
                                // Use NameSystem to get a proper brand name
                                string brandName = "Unknown Brand";
                                if (EntityManager.Exists(brandEntity))
                                {
                                    // Get the rendered name from the NameSystem
                                    brandName = m_NameSystem.GetRenderedLabelName(brandEntity);
                                }
                                
                                brandInfos.Add(new BrandInfo(brandName, brandEntity));
                            }
                        }
                    }
                }
            }

            // Update bindings
            m_AvailableBrandsBinding.Value = brandInfos.ToArray();

            // If we have no selected brand but have available brands, select the first one
            if (m_SelectedBrand == Entity.Null && brandInfos.Count > 0)
            {
                SelectBrand(brandInfos[0]);
            }
            else if (m_SelectedBrand != Entity.Null)
            {
                // Update the selected brand binding with name info
                foreach (var brandInfo in brandInfos)
                {
                    if (brandInfo.entity == m_SelectedBrand)
                    {
                        m_SelectedBrandBinding.Value = brandInfo;
                        break;
                    }
                }
            }
        }
    }
}
