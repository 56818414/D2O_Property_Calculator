# D2O 重水亚临界区物性计算核心（C#）<br>D2O (Heavy Water) Subcritical Thermophysical Properties Core (C#)

> 单文件 C# 实现，基于 IAPWS 官方重水热力学公式 **R16-17(2018)**（《IAPWS Formulation 2017 for the Thermodynamic Properties of Heavy Water Substance》）。
> Single-file C# implementation based on the official IAPWS heavy-water formulation
> **R16-17(2018)** — "IAPWS Formulation 2017 for the Thermodynamic Properties of Heavy Water Substance".
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.22671104.svg)](https://zenodo.org/records/22671104)
---

## 简介 / Overview

本仓库仅包含 **`D2O.cs`** 一个核心算法文件，是论文《面向工程的IAPWS-2017重水亚临界区物性在线计算平台实现》所述物性计算平台的算法核心，也是在线平台 [https://www.calculsite.com](https://www.calculsite.com) 的底层计算引擎。

This repository contains a **single core algorithm file, `D2O.cs`** — the algorithmic
core of the thermophysical-property calculation platform described in the paper
*"Implementation of an Engineering-Oriented Online Computing Platform for IAPWS-2017
Heavy-Water Subcritical Thermophysical Properties"*, and the calculation engine behind the online
platform [https://www.calculsite.com](https://www.calculsite.com).

- **语言 / Language**：C#（.NET 6.0 及以上，已在 .NET 10 验证）
- **依赖 / Dependencies**：仅依赖 `System`，**无第三方依赖，单文件即可编译**
  （Only depends on `System`; no third-party dependencies — compiles as a single file.）
- **许可证 / License**：MIT（见 `LICENSE`）

---

## 实现的标准 / Standard Implemented

| 性质 / Property | 标准 / Standard | 形式 / Form |
|---|---|---|
| 热力学性质 / Thermodynamics | **IAPWS R16-17(2018)**（重水 Heavy Water） | 无量纲 Helmholtz 自由能 α⁰+αʳ，(T, ρ) 为自变量 / dimensionless Helmholtz free energy, (T, ρ) as independent variables |

> 实现依据 IAPWS 官方发布文件 **R16-17(2018)**（《IAPWS Formulation 2017 for the Thermodynamic
> Properties of Heavy Water Substance》）所载公式与系数**独立编写**。论文题名中的“IAPWS-2017”
> 即指该官方重水公式（Formulation 2017，发布为 R16-17(2018)）。
> The implementation is independently written from the formulas and coefficients in the official
> IAPWS release **R16-17(2018)** ("IAPWS Formulation 2017 for the Thermodynamic Properties of
> Heavy Water Substance"). The "IAPWS-2017" in the paper title denotes this official heavy-water
> formulation (Formulation 2017, released as R16-17 in 2018).

---

## 功能 / Features

- **密度求解 / Density solver**：给定 (T, p) 反算密度 ρ，支持 `auto` / `liquid` / `vapor` 相态指定。
  Given (T, p), inverts to density ρ, with `auto` / `liquid` / `vapor` phase selection.
- **饱和压力 / Saturation pressure**：`Psat(T)` 采用割线法（secant / Illinois）并辅以 Clausius–Clapeyron 两点初值。
  Saturation pressure `Psat(T)` via the secant (Illinois) method with a two-point
  Clausius–Clapeyron initial guess.
- **饱和密度 / Saturation densities**：气相、液相密度经 Maxwell 等面积准则求解。
  Vapor/liquid saturation densities via the Maxwell equal-area criterion.
- **相态判定 / Phase determination**：亚临界区内由饱和压力区分液相 / 气相（近临界与超临界不在范围内，见适用范围）。
  Within the subcritical region, liquid and vapor are distinguished by the saturation
  pressure (the near-critical and supercritical regions are out of scope — see Scope).
- **热力学性质 / Thermodynamic properties**：ρ、压缩因子 Z、Cp、Cv、焓 h、熵 s、内能 u、Gibbs 自由能、声速 w。
  ρ, compressibility factor Z, Cp, Cv, enthalpy h, entropy s, internal energy u,
  Gibbs free energy, and speed of sound w.
- **扩展位 / Extension fields**：`D2OResult` 已预留粘度 η、导热系数 λ、Pr 字段，
  本版本暂未填充（transport properties are reserved for extension, not computed in this release）。

### 适用范围 / Scope

| 常数 / Constant | 数值 / Value |
|---|---|
| 临界温度 Tc | 370.697 ℃ (643.847 K) |
| 临界压力 Pc | 21.650382 MPa |
| 临界密度 ρc | 17.77555 mol/dm³ |
| 三相点 / Triple point | 3.82 ℃ |

本实现面向重水（D₂O）的**亚临界区**，覆盖液相、气相单相区与两相（饱和）区：
This implementation targets the **subcritical region** of heavy water (D₂O) — liquid, vapor
single-phase, and two-phase (saturation) regions.

**明确不在范围内（无论计算是否正确，均不涉足）**：
- **近临界区**：温度 ≥ 620 K（约 347 ℃），直至临界点 643.847 K / 370.697 ℃；
- **超临界区**：温度 ≥ 临界点。

**Explicitly out of scope (by design, regardless of computational correctness)**:
- the **near-critical region**: T ≥ 620 K (≈ 347 ℃) up to the critical point 643.847 K / 370.697 ℃;
- the **supercritical region**: T ≥ critical point.

---

## 编译与运行 / Build & Usage

```bash
# 将 D2O.cs 加入任意 C# 工程即可，无需其他文件
# Just add D2O.cs to any C# project; no other files required.
dotnet build
```

---

## 用法示例 / Usage Example

```csharp
using CalculSite.Services.ThermoProperties;

// 1) 给定 (T, p) 计算全部物性 / Calculate all properties from (T, p)
var calc = new D2OProperties();
D2OResult r = calc.Calculate(150.0, 0.5);   // T = 150 ℃, p = 0.5 MPa
Console.WriteLine($"rho={r.rho_mol_per_dm3} mol/dm3, Z={r.Z_Factor}, phase={r.PhaseUsed}");
// 派生单位 / derived units:
double rho_kg_m3 = r.rho_kg_per_m3;          // kg/m³
double h_kJ_kg   = r.h_kJ_per_kg;            // kJ/kg

// 2) 饱和压力 / Saturation pressure at 100 ℃ (Pa)
var d2o = new D2O();
double psat_Pa = d2o.Psat_Pa(100.0);         // ≈ 0.09630731526198 MPa

// 3) 指定相态求密度 / Density with explicit phase
double rhoVap = d2o.Rho_Density_mol_per_dm3(226.85, 0.206052588, "vapor");  // ≈ 0.05 mol/dm³
```

### 主要公有 API / Public API

| 类型 / Type | 成员 / Member | 说明 / Description |
|---|---|---|
| `D2OProperties` | `D2OResult Calculate(double T_Cel, double P_MPa)` | (T, p) → 全物性 / full properties |
| `D2O` | `double Psat_Pa(double T_Cel)` | 饱和压力 (Pa) / saturation pressure |
| `D2O` | `double Rho_Density_mol_per_dm3(double T_Cel, double P_MPa, string phase="auto", bool forSaturation=false)` | (T, p) → 密度 / density |
| `D2OResult` | `rho_mol_per_dm3, Z_Factor, Cp_J_per_mol_K, Cv_J_per_mol_K, h_J_per_mol, s_J_per_mol_K, w_m_per_s, PhaseUsed, rhoVapor_mol_per_dm3, rhoLiquid_mol_per_dm3, …` | 结果字段 / result fields |

---

## 验证 / Validation

本实现的验证**仅**依据官方发布文件 **IAPWS R16-17(2018)** 中的计算机程序验证表（Computer-Program Verification）进行，不采用其他来源数据：

- **Table 6**（T = 500 K、ρ = 46.26 mol·dm⁻³ 处的无量纲 Helmholtz 自由能 α⁰、αʳ 及其一阶、二阶偏导，共 12 项）：与发布值逐项核对，**最大相对偏差 ≈ 4.2×10⁻⁹**。
- **Table 7**（单区 (T, ρ) 处的 p、cᵥ、w、s）：对亚临界区（T < 620 K）的 8 个代表性单区点（T = 300 K 与 500 K）逐项核对，密度反算与相态判定均正确，ρ、cᵥ、w、s **最大相对偏差 ≤ 3.5×10⁻⁹（约 8–9 位有效数字，已达双精度浮点舍入误差量级），可认为与官方发布值一致**。
- **范围一致性**：Table 7 中 T ≥ 620 K 的点（643.8 K、800 K）属近临界 / 超临界区，不在本实现范围内，依论文适用范围不予纳入验证。

Validation is performed **only** against the computer-program verification tables of the official
**IAPWS R16-17(2018)** release — no other data sources are used:

- **Table 6** (α⁰, αʳ and their first- and second-order derivatives at T = 500 K,
  ρ = 46.26 mol·dm⁻³; 12 values): checked one-by-one against the published values,
  **maximum relative deviation ≈ 4.2×10⁻⁹**.
- **Table 7** (p, cᵥ, w, s at selected (T, ρ) in the single-phase region): for the 8
  subcritical points (T = 300 K and 500 K, i.e. T < 620 K), density inversion and phase
  selection are correct; **maximum relative deviations of ρ, cᵥ, w, s are ≤ 3.5×10⁻⁹
  (≈8–9 significant digits, i.e. at the level of double-precision round-off), in
  agreement with the published values**.
- **Scope consistency**: Table 7 points at T ≥ 620 K (643.8 K, 800 K) lie in the
  near-critical / supercritical region and are excluded from verification, consistent
  with the stated scope.

---

## 引用 / Citation

如使用本代码，请引用关联论文与归档：

If you use this code, please cite the associated paper and archive:

```
常彦斌. 面向工程的IAPWS-2017重水亚临界区物性在线计算平台实现（待发表）. 2026.
Chang, Yanbin. Implementation of an Engineering-Oriented Online Computing Platform for
IAPWS-2017 Heavy-Water Subcritical Thermophysical Properties (in preparation). 2026.

Software archive (Zenodo): DOI 10.5281/zenodo.22671104
                           https://doi.org/10.5281/zenodo.22671104
Online platform: https://www.calculsite.com
```

> 注：关联论文目前**尚未正式发表**，故按"待发表（in preparation）"标注，未使用 `[J]`（已发表期刊）格式。论文正式录用/刊出后，请补充期刊名称、卷期、页码与正式 DOI，并将"待发表"改为标准 `[J]` 引用。

---

## 版权与许可 / Copyright & License

- **代码许可 / Code license**：MIT（见 `LICENSE`）。
- **IAPWS 文件版权**：IAPWS 发布文件本身的版权归 IAPWS 所有，**未随本包分发**；
  获取方式见 <https://iapws.org>。
  The IAPWS publications are copyrighted by IAPWS and are **NOT redistributed** with this
  package; obtain them from <https://iapws.org>.

---

## 免责声明 / Disclaimer

本软件按"原样"提供，作者不对工程应用中的使用后果承担责任。
This software is provided "as is", without warranty of any kind.
