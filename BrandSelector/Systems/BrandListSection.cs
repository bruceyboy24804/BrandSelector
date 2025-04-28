using System;
using BrandSelector.Extensions;
using Colossal.Entities;
using Colossal.UI.Binding;
using Unity.Entities;
using Game.Buildings;
using Game.Prefabs;
using System.Collections.Generic;
using Colossal.Logging;
using BrandSelector.Domain;
using Game.Common;
using Unity.Collections;

namespace BrandSelector.Systems
{


    public partial class BrandListSection : ExtendedInfoSectionBase
    {
        protected override string group => "BrandListSection";
        private ValueBindingHelper<BrandInfo[]> m_AvailableBrandInfos;
        private ValueBindingHelper<BrandInfo> m_SelectedBrandInfo;
        private EntityQuery m_CompaniesQuery;
        private Entity m_PreviousSelection;
        private ILog m_Log;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_PreviousSelection = Entity.Null;
            m_Log = Mod.log;
            
            // Create bindings
            m_AvailableBrandInfos = CreateBinding("availableBrands", Array.Empty<BrandInfo>());
            m_SelectedBrandInfo = CreateBinding("selectedBrand", "selectBrand", new BrandInfo("", Entity.Null), SelectBrand);

            CreateTrigger<BrandInfo>("selectBrand", SelectBrand);

            m_CompaniesQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Companies.CompanyData, PrefabRef>()
                .WithNone<Deleted, Game.Tools.Temp>()
                .Build();
        }

        private bool Visible()
        {
            if (EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Renter> renterBuffer) &&
                renterBuffer.Length > 0)
            {
                for (int i = 0; i < renterBuffer.Length; i++) {
                    if (EntityManager.TryGetComponent(renterBuffer[i], out PrefabRef prefabRef) &&
                        prefabRef.m_Prefab != Entity.Null &&
                        EntityManager.HasBuffer<CompanyBrandElement>(prefabRef.m_Prefab) &&
                        EntityManager.HasComponent<Game.Companies.CompanyData>(renterBuffer[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void OnUpdate()
        {
	        base.visible = Visible();
        }
        
        private void SelectBrand(BrandInfo brandInfo)
        {
            m_SelectedBrandInfo.Value = brandInfo;
            
            // Update the renter to the selected brand
            if (selectedEntity != Entity.Null &&
                EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Renter> renterBuffer) &&
                renterBuffer.Length > 0)
            {
                EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
                for (int i = 0; i < renterBuffer.Length; i++)
                {
                    if (EntityManager.TryGetComponent(renterBuffer[i], out PrefabRef prefabRef) &&
                        prefabRef.m_Prefab != Entity.Null &&
                        EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<CompanyBrandElement> companyBrandElements) &&
                        EntityManager.HasComponent<Game.Companies.CompanyData>(renterBuffer[i]))
                    {
                        Dictionary<Entity, List<Entity>> compatibleCompanies;
                        if (TryFindCompatibleCompanies(prefabRef, out compatibleCompanies) &&
                            compatibleCompanies.ContainsKey(brandInfo.Entity))
                        {
                            DynamicBuffer<Renter> newRenters = buffer.SetBuffer<Renter>(selectedEntity);
                            newRenters.Clear();
                            newRenters.Add(new Renter { m_Renter = compatibleCompanies[brandInfo.Entity][0] });
                            m_Log.Debug($"{nameof(BrandListSection)}.{nameof(SelectBrand)} assigned new renter {compatibleCompanies[brandInfo.Entity][0]}");
                            buffer.AddComponent(selectedEntity, new RentersUpdated(selectedEntity));

                        }

                        break;
                    }
                }
            }
            
            RequestUpdate();
        }

        
        protected override void Reset()
        {
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
        }

        protected override void OnProcess()
        {
            if (m_PreviousSelection != selectedEntity && selectedEntity != Entity.Null)
            {
                m_PreviousSelection = selectedEntity;

                ProcessAvailableBrands();
            }
        }

        private bool TryFindCompatibleCompanies(PrefabRef companyPrefabEntity, out Dictionary<Entity, List<Entity>> compatibleCompanies)
        {
            NativeArray<Entity> companies = m_CompaniesQuery.ToEntityArray(Allocator.Temp);
            compatibleCompanies = new Dictionary<Entity, List<Entity>>();
            for (int i=0; i < companies.Length; i++)
            {
                if (EntityManager.TryGetComponent(companies[i], out PrefabRef prefabRef) &&
                    prefabRef.m_Prefab != Entity.Null &&
                    prefabRef.m_Prefab == companyPrefabEntity &&
                    EntityManager.TryGetComponent(companies[i], out Game.Companies.CompanyData companyData))
                {
                    if (!compatibleCompanies.ContainsKey(companyData.m_Brand))
                    {
                        compatibleCompanies.Add(companyData.m_Brand, new List<Entity>());
                    }

                    compatibleCompanies[companyData.m_Brand].Add(companies[i]);
                }
            }

            if (companies.Length > 0)
            {
                return true;
            }
            return false;
        }

        private void ProcessAvailableBrands()
        {
            List<BrandInfo> brandInfos = new List<BrandInfo>();
            // Get current renter to identify selected brand
            if (EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Renter> renterBuffer) &&
                renterBuffer.Length > 0)
            {
                for (int i = 0; i < renterBuffer.Length; i++)
                {
                    if (EntityManager.TryGetComponent(renterBuffer[i], out PrefabRef prefabRef) &&
                        prefabRef.m_Prefab != Entity.Null &&
                        EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<CompanyBrandElement> companyBrandElements) &&
                        EntityManager.TryGetComponent(renterBuffer[i], out Game.Companies.CompanyData companyData))
                    {
                        Dictionary<Entity, List<Entity>> compatibleCompanies;
                        if (!TryFindCompatibleCompanies(prefabRef, out compatibleCompanies))
                        {
                            break;
                        }

                        for (int j = 0; j < companyBrandElements.Length; j++)
                        {

                            m_Log.Debug($"{nameof(BrandListSection)}.{nameof(ProcessAvailableBrands)} found a brand.");

                            // Use NameSystem to get a proper brand name
                            string brandName = "Unknown Brand";
                            if (EntityManager.Exists(companyBrandElements[j].m_Brand))
                            {
                                // Get the rendered name from the NameSystem
                                brandName = m_NameSystem.GetRenderedLabelName(companyBrandElements[j].m_Brand);
                            }
                            BrandInfo brandInfo = new BrandInfo(brandName, companyBrandElements[j].m_Brand);

                            if (compatibleCompanies.ContainsKey(companyBrandElements[j].m_Brand))
                            {
                                brandInfos.Add(brandInfo);

                                m_Log.Debug($"{nameof(BrandListSection)}.{nameof(ProcessAvailableBrands)} added brand: {brandName} {companyBrandElements[j].m_Brand.Index} {companyBrandElements[j].m_Brand.Version}.");

                                if (companyBrandElements[j].m_Brand == companyData.m_Brand)
                                {
                                    m_SelectedBrandInfo.Value = brandInfo;
                                    m_SelectedBrandInfo.Binding.TriggerUpdate();
                                }
                            } else
                            {
                                m_Log.Debug($"{nameof(BrandListSection)}.{nameof(ProcessAvailableBrands)} no compatible companies. did not add brand: {brandName} {companyBrandElements[j].m_Brand.Index} {companyBrandElements[j].m_Brand.Version}.");
                                if (companyBrandElements[j].m_Brand == companyData.m_Brand)
                                {
                                    m_Log.Info($"{nameof(BrandListSection)}.{nameof(ProcessAvailableBrands)} company is not valid for selected brand!!!");
                                }
                            }
                        }

                        break;
                    }
                }
                    
            }

            m_AvailableBrandInfos.Value = brandInfos.ToArray();
            m_AvailableBrandInfos.Binding.TriggerUpdate();
            RequestUpdate();
        }
    }
}