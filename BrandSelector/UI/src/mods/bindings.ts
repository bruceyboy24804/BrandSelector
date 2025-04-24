import { bindValue, bindTrigger } from "cs2/api";
import { Entity } from "cs2/bindings";
import mod from "mod.json";

// Define the BrandInfo interface to match the C# struct
export interface BrandInfo {
  name: string;
  entity: Entity;
}

export const availableBrands$ = bindValue<BrandInfo[]>(mod.id, "availableBrands");
export const selectedBrand$ = bindValue<BrandInfo>(mod.id, "selectedBrand");
