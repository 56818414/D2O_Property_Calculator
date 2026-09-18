//using CalculSite.Components.Pages;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
//using Microsoft.AspNetCore.Routing.Constraints;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Reflection.Metadata.Ecma335;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Runtime.Versioning;
//using System.Security.Cryptography.Xml;
//using System.Transactions;



namespace CalculSite.Services.ThermoProperties
{
    /// <summary>
    /// D2O 的热力学性质计算
    /// 文献：Revised Release on the IAPWS Formulation 2017 for the Thermodynamic  Properties of Heavy Water
    /// 编码:IAPWS R16-17(2018)
    /// D2O 的传输性质计算
    /// 文献：Release on the IAPWS Formulation 2021 for the Thermal Conductivity of Heavy  Water
    /// 编码:IAPWS R18-21
    /// 文献：Release on the IAPWS Formulation 2020 for the Viscosity of Heavy Water
    /// 编码:IAPWS R17-20
    /// </summary>

    ///<summary>
    /// 结果容器
    /// </summary>    
    public class D2OResult
    {
        // 0. 状态参数
        public double Temperature_Cel { get; set;  }
        public double Pressure_MPa {  get; set; }
        public string PhaseUsed { get; set;  } // 液态 或 气态

        // 1. 基本热力学性质
        // 摩尔基准
        public double rho_mol_per_dm3 { get; set;  }             
        public double Z_Factor { get; set;  }
        public double u_J_per_mol { get; set;  }
        public double h_J_per_mol { get; set; }
        public double s_J_per_mol_K {  get; set; }
        public double Gibbs_J_per_mol { get; set; }
        public double Cv_J_per_mol_K { get; set; }
        public double Cp_J_per_mol_K { get; set; }
        public double w_m_per_s {  get; set; }

        //质量基准
        public double rho_kg_per_m3 => rho_mol_per_dm3 * MoleD2O_g_mol;
        public double u_kJ_per_kg => u_J_per_mol / MoleD2O_g_mol;
        public double h_kJ_per_kg => h_J_per_mol / MoleD2O_g_mol;
        public double s_kJ_per_kg_K => s_J_per_mol_K / MoleD2O_g_mol;
        public double Cv_kJ_per_kg_K => Cv_J_per_mol_K / MoleD2O_g_mol;
        public double Cp_kJ_per_kg_K => Cp_J_per_mol_K / MoleD2O_g_mol;

        //辅助参数
        public double? Psat_MPa { get; set; }
        public double? rhoVapor_mol_per_dm3 { get; set; }
        public double? rhoLiquid_mol_per_dm3 { get; set; }
        public double? rhoVapor_kg_per_m3 => rhoVapor_mol_per_dm3 * MoleD2O_g_mol;
        public double? rhoLiquid_kg_per_m3 => rhoLiquid_mol_per_dm3 * MoleD2O_g_mol;

        //传输性质
        //待实现
        public double eta_muPa_s {  get; set; }
        public double nu_cSt {  get; set; }
        public double lambda_mW_per_m_K { get; set; }
        public double Pr_D2O { get; set; }
                      
        //常数

        internal const double Tc_K = 643.847;// critical temperature Tc 温度K
        internal const double Rhoc_mol_per_dm3 = 17.77555;// cricical density 密度：mol/dm3
        internal const double R_J_per_mol_K = 8.3144598;//气体常数：J/(mol.K)
        internal const double MoleD2O_g_mol = 20.027508;//摩尔质量，g/mol
        internal const double Tt_K = 276.969;//triple-point temperature 

        // 传输性质 相关参数
        internal const double LAMBDA = 175.9870; //常数
        internal const double xi0_nm = 0.13;
        internal const double qD_nm = 0.36;
        internal const double Gamma = 0.06;

        internal const double T_star = 643.847;// reference temperature 参考温度 K
        internal const double P_star = 21.6618;// reference pressure 参考压力 MPa
        internal const double Rho_star = 356.0;// reference density 参考密度 kg/m3
        internal const double lambda_star = 1e-3;// reference thermal conductivity 参考热导率 1×10^-3 W/m.K
        internal const double mu_star = 1e-6;// reference viscosity 参考粘度 1×10^-6 Pa.s
        internal const double R_star = 0.41515199;// specific gas constant 比气体常数 kJ/kg.K




        //输出
        
        internal D2OResult(double T_Cel,double P_MPa,string phase,double rho,double Z,double u,double h,double s,double g,double cv,double cp,double w, double? Psat, double? rhoV, double? rhoL)
        {
            Temperature_Cel = T_Cel;
            Pressure_MPa= P_MPa;
            PhaseUsed = phase;
            rho_mol_per_dm3 = rho;
            Z_Factor = Z;
            u_J_per_mol = u;
            h_J_per_mol = h;
            s_J_per_mol_K= s;
            Gibbs_J_per_mol = g;
            Cv_J_per_mol_K = cv;
            Cp_J_per_mol_K = cp;
            w_m_per_s = w;
            Psat_MPa= Psat;
            rhoVapor_mol_per_dm3 = rhoV;
            rhoLiquid_mol_per_dm3 = rhoL;
            
        }


    }


    // 2. 外观调用
    public class D2OProperties
    {
        private readonly D2O _calculator;
        public D2OProperties()
        {
            _calculator = new D2O();
        }

        ///<summary>
        /// 计算指定温度、压力下的D2O的全部热力学性质（自动判断相态）
        /// </summary>
        public D2OResult Calculate(double T_Cel,double P_MPa)
        {
            //1. 判断相态
            string phase = DeterminePhase(T_Cel, P_MPa);

            //2. 对比参数计算
            var state = _calculator.ComputeThermoState(T_Cel, P_MPa, phase);

            double R = D2OResult.R_J_per_mol_K;
            double T_K = T_Cel + 273.15;
            double RT = R * T_K;
            double delta=state.delta,tau=state.tau;
            double rho=state.rho;

            //3. 摩尔基准性质
            double z = 1.0 + delta * state.ar_d;
            double u = RT * tau * (state.a0_t + state.ar_t);
            double s = R * (tau * (state.a0_t + state.ar_t) - state.a0 - state.ar);
            double h = RT * (1.0 + tau * ((state.a0_t + state.ar_t)) + delta * state.ar_d);
            double g = RT * (1.0 + state.a0 + state.ar + delta * state.ar_d);//参考N2的吉布斯标准函数公式
            double cv = -R * (tau * tau * (state.a0_tt + state.ar_tt));
            //-------  cp计算  --------
            double denom = 1.0 + 2.0 * delta * state.ar_d + delta * delta * state.ar_dd;   //分母
            double numerator = 1.0 + delta * state.ar_d - delta * tau * state.ar_dt;            //分子
            double cp = cv + R * (numerator * numerator) / denom;

            double w = 0.0;
            double M = D2OResult.MoleD2O_g_mol / 1000.0;
            double W2 = (RT / M) * (denom - (numerator * numerator) / (tau * tau * (state.a0_tt + state.ar_tt)));
            if (W2 > 0) w = Math.Sqrt(W2);

            //4. 饱和结果输出
            double? psat = null, rhoV = null, rhoL = null;
            if (T_K < D2OResult.Tc_K)
            {
                try
                {
                    double psat_Pa = _calculator.Psat_Pa(T_Cel);
                    psat = psat_Pa * 1e-6;
                    rhoV = _calculator.Rho_Density_mol_per_dm3(T_Cel, psat.Value, "vapor");
                    rhoL = _calculator.Rho_Density_mol_per_dm3(T_Cel, psat.Value, "liquid");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[D2OProperties.Calculate] 饱和信息获取失败 (t={T_Cel:F2}°C): {ex.Message}");
                }
            }
            return new D2OResult(T_Cel, P_MPa, phase, rho, z, u, h, s, g, cv, cp, w, psat, rhoV, rhoL);

        }
        //辅助函数，判断相态
        private string DeterminePhase(double T_Cel,double P_MPa)
        {
            double T_K = T_Cel + 273.15;
            if (T_K >= D2OResult.Tc_K)
            {
                double pCritIso_MPa = _calculator.Ps_from_rho_Pa(D2OResult.Rhoc_mol_per_dm3, T_K) / 1E6;
                return (P_MPa >= pCritIso_MPa) ? "liquid" : "vapor";
            } 
                
            double psat = _calculator.Psat_Pa(T_Cel) * 1E-6;
            double tol = Math.Max(1e-8, 1e-6 * psat);
            if (P_MPa > psat + tol) return "liquid";
            if (P_MPa < psat - tol) return "vapor";
            throw new ArgumentException("状态位于两相区，请重新选择温度/压力区间");
        }
     }


    
    public class D2O
    {
        //==================================== 1. 基本数据 ========================

        /// <summary>
        /// 定义数据格式
        /// </summary>
        // 定义理想气体数据格式
        public class IdealGas
        {
            public double c0 { get; set; }
            public double a1 { get; set; }
            public double a2 { get; set; }
            public double[] vi { get; set; }
            
            public double[] ui { get; set; }
            
        }
        //定义 剩余气体 数据格式
        public class ResidualGas
        {
            public double[] ni { get; set; }
            public double[] ti { get; set; }
            public int[] di { get; set; }
            public int[] li { get; set; }
            public double[] etai { get; set; }
            public double[] betai { get; set; }
            public double[] gammai { get; set; }
            public double[] epsiloni { get; set; }
        }

       

        //==================================== 2. 输入参数 =========================
        /// <summary>
        /// 输入理想气体参数数据
        /// Table1
        /// </summary>
        private class IdealData : IdealGas
        {
            public IdealData()
            {
                c0 = 4.0;
                a1 = -8.67099402264600;
                a2 = 6.96033578458778;
                vi = new double[5]
                {
                    0.0,
                    0.10633e-1,
                    0.99787,
                    0.21483e1,
                    0.3549
            };
                ui = new double[5]
                {
                    0.0,
                    308.0,//单位K
                1695.0,//单位K
                 3949.0,//单位K
                10317.0//单位K
            };
               
            }
        }
        /// <summary>
        /// 输入剩余部分气体参数数据
        /// Table2
        /// </summary>
        private class ResidualData : ResidualGas
        {
            public ResidualData()
            {
                ni = new double[25]
                    {
                    //0 - 5
                    0.0,
                    0.122082060e-1,
                    0.296956870e1,
                    -0.379004540e1,
                    0.941089600,
                    -0.922466250,
                    //6 - 10
                    -0.139604190e-1,
                    -0.125203570,
                    -0.555391500e1,
                    -0.493009740e1,
                    -0.359470240e-1,
                    //11 - 15
                    -0.936172870e1,
                    -0.691835150,
                    -0.456110600e-1,
                    -0.224513300e1,
                    0.860006070e1,
                    //16 - 20
                    -0.248410420e1,
                    0.164476900e2,
                    0.270393360e1,
                    0.375637470e2,
                    -0.177607760e1,
                    //21 - 24
                    0.220924640e1,
                    0.519652000e1,
                    0.421097400,
                    -0.391921100,
                    };
                ti = new double[25]
                {
                        // 0 - 5
                        0.0, 1.0,0.6555,0.9369,0.5610,0.7017,
                        // 6 - 10
                        1.0672,3.9515,4.6,5.159,0.2,
                        // 11 - 15
                        5.4644,2.3660,3.4553,1.4150,1.5745,
                        // 16 - 20
                        3.4540,3.8106,4.8950,1.4300,1.5870,
                        // 21 - 24
                        3.790,2.620,1.90,4.32
                };
                di = new int[25]
                {
                        // 0 -5  
                        0,4,1,1,2,2,
                        // 6 - 10
                        3,1,1,3,2,
                        // 11 - 15
                        2,1,1,3,1,
                        // 16 - 20
                        3,1,1,2,2,
                        // 21 - 24
                        2,1,1,1,
                };
                li = new int[25]
                {
                        // 0 -5  
                        0,0,0,0,0,0,
                        // 6 - 10
                        0,1,2,2,1,
                        // 11 - 15
                        2,2,0,0,0,
                        // 16 - 20
                        0,0,0,0,0,
                        // 21 - 24
                        0,0,0,0,
                };
                etai = new double[25]
                {
                        // 0 - 5
                        0.0,0.0,0.0,0.0,0.0,0.0,
                        // 6 - 10
                        0.0,0.0,0.0,0.0,0.0,
                        // 11 - 15
                        0.0,
                        0.0,
                        0.6014,
                        1.4723,
                        1.5305,
                        // 16 - 20
                        2.4297,
                        1.3086,
                        1.3528,
                        3.4456,
                        1.2645,
                        // 21 - 24
                        2.5547,
                        1.2148,
                        18.738,
                        18.677,
                };
                betai = new double[25]
                {
                    // 0
                    0.0,
                    // 1 - 5
                    0.0,0.0,0.0,0.0,0.0,
                    // 6 - 10
                    0.0,0.0,0.0,0.0,0.0,
                    // 11 - 15
                    0.0,0.0,0.4200,2.4318,1.2888,
                    // 16 - 20
                    8.2710,
                    0.3673,
                    0.9504,
                    7.8318,
                    3.3281,
                    // 21 - 25
                    7.1753,
                    0.9465,
                    1177.0,
                    1167.0
                };
                gammai = new double[25]
                {
                    // 0
                    0.0,
                    // 1 - 5
                    0.0,0.0,0.0,0.0,0.0,
                    // 6 - 10
                    0.0,0.0,0.0,0.0,0.0,
                    // 11 - 15
                    0.0,
                    0.0,
                    1.5414,
                    1.3794,
                    1.7385,
                    // 16 - 20
                    1.3045,
                    2.7242,
                    3.5321,
                    2.4552,
                    0.8319,
                    // 21 - 25
                    1.3500,
                    2.5617,
                    1.0491,
                    1.0486
                };
                epsiloni = new double[25]
                {
                    // 0
                    0.0,
                    // 1 - 5
                    0.0,0.0,0.0,0.0,0.0,
                    // 6 - 10
                    0.0,0.0,0.0,0.0,0.0,
                    // 11 - 15
                    0.0,
                    0.0,
                    1.8663,
                    0.2895,
                    0.5803,
                    // 16 - 20
                    0.2236,
                    0.6815,
                    0.9495,
                    1.1158,
                    0.1607,
                    // 21 - 25
                    0.4144,
                    0.9683,
                    0.9488,
                    0.9487
                };
            }
        }
        // 输入 粘度系数 Hij

        public class ViscosityCoefficients
        {
            private static readonly (int i, int j, double Hij)[] H_ij_Table = new (int, int, double)[]
            {
                // j=0
                (0,0,0.510953),
                (2,0,-0.558947),
                (3,0,-2.718820),
                (4,0,0.480990),
                (5,0,2.404510),
                (6,0,-1.824320),

                // j=1
                (0,1,0.275847),
                (1,1,0.762957),
                (3,1,1.760340),
            };
        }

        // =================================== 3. 定义常数 ==========================
        private double Tc_K = 643.847;// critical temperature Tc 温度K
        private double Rhoc_mol_per_dm3 = 17.77555;// cricical density 密度：mol/dm3
        private double R_J_per_mol_K = 8.3144598;//气体常数：J/(mol.K)
        private double MoleD2O_g_mol = 20.027508;//摩尔质量，g/mol
        private double Tt_K = 276.969;//triple-point temperature 

        // 理想气体参数
        private IdealData _idealData;
        // 剩余参数
        private ResidualData _residualData;

        // =================================== 4.构造函数 ===========================
        public D2O()
        {
            _idealData=new IdealData();
            _residualData=new ResidualData();
        }


        // =================================== 5. 各物性函数 =========================
        // 2. 通用计算器
        internal class ThermoState
        {
            public double rho;
            public double delta, tau;
            public double a0, a0_d, a0_t, a0_dd, a0_tt, a0_dt;//理想部分
            public double ar, ar_d, ar_t, ar_dd, ar_tt, ar_dt;//剩余部分
        }
        internal ThermoState ComputeThermoState(double T_Cel,double P_MPa,string phase)
        {
            double rho = Rho_Density_mol_per_dm3(T_Cel, P_MPa, phase);
            double T_K = T_Cel + 273.15;
            double delta = rho / Rhoc_mol_per_dm3;
            double tau = Tc_K / T_K;

            return new ThermoState
            {
                rho=rho,//直接存在rho
                delta = delta,
                tau = tau,
                a0 = alpha0(tau, delta),
                a0_d = alpha0_delta(delta),
                a0_t = alpha0_tau(tau),
                a0_dd = alpha0_delta_delta(delta),
                a0_tt = alpha0_tau_tau(tau),
                a0_dt = alpha0_delta_tau(),

                ar = alphaR(tau, delta),
                ar_d = alphaR_delta(tau, delta),
                ar_t = alphaR_tau(tau, delta),
                ar_dd = alphaR_delta_delta(tau, delta),                   
                ar_tt = alphaR_tau_tau(tau, delta),
                ar_dt = alphaR_delta_tau(tau, delta)
            };
        }
        
        // 1. 计算密度
        /// <summary>
        /// 辅助函数，压力对密度导数，用于密度计算
        /// </summary>
        /// <param name="rho"></param>
        /// <param name="T_K"></param>
        /// <returns></returns>
        private double dP_drho(double rho, double T_K)
        {
            double h = 1e-6 * Math.Max(Math.Abs(rho), 1e-6);
            double rho1 = rho + h;
            double rho2 = rho - h;
            double P1 = Ps_from_rho_Pa(rho1, T_K);
            double P2 = Ps_from_rho_Pa(rho2, T_K);
            return (P1 - P2) / (2.0 * h);
        }
        /// <summary>
        /// 计算密度
        /// </summary>
        /// <param name="T_Cel">温度单位（℃）</param>
        /// <param name="P_MPa">压力单位（MPa）</param>
        /// <param name="phase">相态（auto）</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public double Rho_Density_mol_per_dm3(double T_Cel, double P_MPa, string phase = "auto",bool forSaturation=false)
        {
            //1. 数据预处理
            double R = R_J_per_mol_K;
            double T_K = T_Cel + 273.15;
            double targetP_Pa = P_MPa * 1e6;
            double tau = Tc_K / T_K;
            double Rhoc = Rhoc_mol_per_dm3;

            // 处理非正压力
            if (targetP_Pa <= 0) targetP_Pa = 1e-6;   // 1 μPa

            double rho;            // 迭代初值
            double rhoMin, rhoMax; // 物理允许区间


            //1.3 根据相态确定初始密度区间

            if (phase == "liquid")
            {
                // 液相初始：高密度，必须 > Rhoc
                rho = Rhoc * 2.6;
                rhoMin = Math.Max(Rhoc * 2.0, Rhoc*2.5);
                rhoMax = 75.0;      // 极高密度不会超过此值

                // 修正2026-09-08：求饱和态时，上述固定下限 Rhoc*2.5=44.44 mol/dm3
                // 在 t> 约 248 ℃ 时，已高于 真实 饱和液相密度(250℃ 时 取 44.12）
                // 导致液相求解失败。改用对应态估计给出随温度收缩的区间。
                if(forSaturation){
                    double tauSat=Math.Max(1.0-T_K/Tc_K,1e-12);
                    double rhoLest=Rhoc*(1.0+2.711*Math.Pow(tauSat,0.357));
                    rho=rhoLest;
                    rhoMin=Math.Max(Rhoc*1.02,rhoLest*0.85);
                    rhoMax=Math.Min(75.0,rhoLest*1.20);
                }
            }
            else             //vapor
            {
                // 气相初始：理想气体估算，必须 < Rhoc
                double rho_ideal = targetP_Pa / (R * T_K) / 1000.0;
                rhoMin = Math.Max(rho_ideal * 1e-6, 1e-12);          //MIN是理想值的1e-6.
                rhoMax = Rhoc * 0.79;
                rho = Math.Min(Math.Max(rho_ideal, rhoMin), rhoMax);

                // 修正2026-09-08：求饱和态时，固定上限 RhoC*0.79=14.04 mol/dm3
                // 在 t> 370℃时 低于真实饱和气相密度 。
                // 改为以理想气体密度为核心、随压力收缩的区间。
                if(forSaturation){
                    rhoMin=Math.Max(1e-12,rho_ideal*0.20);
                    rhoMax = Math.Min(Rhoc * 0.95, rho_ideal * 8.0);
                    rho = Math.Min(Math.Max(rho_ideal * 1.5, rhoMin), rhoMax);
                }
            }

            if (rhoMax <= rhoMin) rhoMax = rhoMin * 10.0 + 1e-12;

            //1.1 过程输出
            double pLow = Ps_from_rho_Pa(rhoMin, T_K);
            double pHigh = Ps_from_rho_Pa(rhoMax, T_K);
            
            if (targetP_Pa <= pLow)
            {
                //Console.WriteLine($"  [Rho]{phase}:targetP={P_MPa}MPa <= pLow={pLow / 1E6}MPa,rhoLow={rhoMin}");
                //return rhoMin;
            }
            if (targetP_Pa >= pHigh)
            {
                //Console.WriteLine($"  [Rho]{phase}:targetP={P_MPa}MPa >= pHigh={pHigh / 1e6}MPa,rhoHigh={rhoMax}");

                //return rhoMax;
            }
            

            //2. 牛顿迭代
            double lambda = 1.0;//步长阻尼
            for (int iter = 0; iter < 200; iter++)
            {                       
                double P_calc = Ps_from_rho_Pa(rho, T_K);
                if (double.IsNaN(P_calc) || double.IsInfinity(P_calc))
                    break;   // 发生数值异常，退出用后备值
                double f = P_calc - targetP_Pa;
                if (Math.Abs(f) < Math.Max(1e-10, 1e-9 * targetP_Pa))  // 高精度容差
                {
                    //Console.WriteLine($"  [Rho]{phase} convered: rho={rho:f6},牛顿迭代iter={iter}");
                    return rho;
                }                                          

                double dP = dP_drho(rho, T_K);
                if (Math.Abs(dP) < 1e-6) dP = 1e-6; // 防止除零

                
                double delta = -f / dP;

                // 限制步长，禁止跳出相态区间
                double rho_new = rho + delta * lambda;
                if (rho_new < rhoMin) rho_new = rhoMin * 0.9 + rho * 0.1; // 向区间内拉回
                if (rho_new > rhoMax) rho_new = rhoMax * 0.9 + rho * 0.1;

                // 如果残差反而增大，减小步长
                double P_new = Ps_from_rho_Pa(rho_new, T_K);
                if (!double.IsNaN(P_new) && Math.Abs(P_new - targetP_Pa) > Math.Abs(f))
                    lambda *= 0.5;
                else
                    lambda = Math.Min(1.0, lambda * 1.2);

                // 防止 delta 过小不更新
                if (Math.Abs(rho_new - rho) < 1e-15 * Math.Max(rho, 1e-10))
                    break;

                rho = rho_new;

            }
            // 3. 增加二分法
            double rhoLow = rhoMin, rhoHigh = rhoMax;
            
            // 修正 2026-09-08：求饱和态时不扩张区间。

            if(!forSaturation){

                for(int expand=0;expand < 20; expand++)
            {
                double p_Low=Ps_from_rho_Pa(rhoLow,T_K);
                double p_High=Ps_from_rho_Pa(rhoHigh,T_K);

                double errLow = p_Low - targetP_Pa;            //正常≤0
                double errHigh = p_High - targetP_Pa;          //正常≥0

                if (errLow * errHigh <= 0)
                {
                    break;//异号返回
                }

                if (targetP_Pa < pLow)
                {
                    rhoLow = phase == "liquid" ? Math.Max(Rhoc * 1.001, rhoLow * 0.95) : Math.Max(1e-16, rhoLow * 0.5);
                }
                else
                {
                    rhoHigh = phase == "liquid" ? Math.Min(90.0, rhoHigh * 1.05) : Math.Min(Rhoc * 0.99, rhoHigh * 1.5);
                }

                /*
                if (errLow>0)//目标压力比密度下界限还低  ，errLow发生大于0，errlow*errhigh同号。调整下限；否则，调整下限，确保errhigh<0。和之前的上下限一样。
                {
                    rhoLow = Math.Max(phase == "vapor" ? 1e-16 : Rhoc, rhoLow * 0.1);//
                }
                else
                {
                    rhoHigh = Math.Min(phase == "vapor" ? rho * 0.99999 : 90.0, rhoHigh * 2.0);
                }
                */
            }

            } // end if(!forSaturation)
            
            // 修正：
            {
                double eL=Ps_from_rho_Pa(rhoLow,T_K)-targetP_Pa;
                double eH=Ps_from_rho_Pa(rhoHigh,T_K)-targetP_Pa;
                if(!(eL*eH<=0)) return double.NaN;
            }

            //二分法
            for(int iter = 0; iter < 200; iter++)
            {
                double rhoMid = (rhoLow + rhoHigh) / 2.0;
                double pMid = Ps_from_rho_Pa(rhoMid, T_K);
                double errMid = pMid - targetP_Pa;

                if(Math.Abs(errMid)<1e-8*Math.Max(1.0,targetP_Pa) || Math.Abs(rhoHigh - rhoLow) < 1e-15)
                {
                    //Console.WriteLine($"  [Rho]{phase} 二分法 iter={iter},bisection : rho={rhoMid:f6},rhoLow={rhoLow},rhoHigh{rhoHigh}");
                    return rhoMid;
                }

                if (pMid < targetP_Pa)
                {
                    rhoLow = rhoMid;
                }
                else
                {
                    rhoHigh = rhoMid;
                }

            }
            /*
            double res = (rhoLow + rhoHigh) / 2.0;
            */
            //Console.WriteLine($"  [Rho]{phase} Fallback: rho={res:f6}");
            //4 .仍旧失败

            return double.NaN;
            /*
            if (phase == "liquid")
                return Rhoc * 1.2;                     // 典型液相密度，确保 > Rhoc
            else
                return Math.Max(1e-14, targetP_Pa / (R * T_K) / 1000.0); // 理想气体
                */
        }
        
        /// <summary>
        /// 测试通用Ps
        /// </summary>
        /// <param name="rhoLocal">状态密度</param>
        /// <returns></returns>
        public double Ps_from_rho_Pa(double rhoLocal,double T_K)
        {
            double Rhoc = Rhoc_mol_per_dm3;
            double R = R_J_per_mol_K;
            double tau = Tc_K / T_K;
            
            double delta = rhoLocal / Rhoc;
            return 1000.0 * R * T_K * rhoLocal * (1.0 + delta * alphaR_delta(tau, delta));
        }
        /// <summary>
        /// 测试通用 Ps
        /// </summary>
        /// <param name="rhoVapor">气相密度</param>
        /// <param name="rhoLiquid">液相密度</param>
        /// <param name="T_K">K氏温标</param>
        /// <returns></returns>
        private double Ps_from_Chem(double rhoVapor, double rhoLiquid,double T_K)
        {
            double Rhoc = Rhoc_mol_per_dm3;
            double R = R_J_per_mol_K;         
            double tau = Tc_K / T_K;

            // 硬性保护，防止零或负密度
            const double MIN_RHO = 1e-10;
            double rhoV = Math.Max(rhoVapor, MIN_RHO);
            double rhoL = Math.Max(rhoLiquid, MIN_RHO);

            // 必须保证液相密度大于气相
            if (rhoL <= rhoV) rhoL = rhoV * 1.001;

            double deltaVapor = rhoVapor / Rhoc;
            double deltaLiquid = rhoLiquid / Rhoc;

            // 公式左侧
            double term1 = (1.0 / rhoVapor - 1.0 / rhoLiquid);
            // 公式右侧
            double term2 = Math.Log(rhoLiquid / rhoVapor);   //移到右侧，符号变正   
            double term3 = alphaR(tau, deltaLiquid) - alphaR(tau, deltaVapor);

            // 防止 term1 为 0（理论上不会）
            if (Math.Abs(term1) < 1e-30) term1 = 1e-30;

            return 1000.0 * R * T_K * (term2 + term3) / term1;

        }
        public double Psat_Pa(double T_Cel)
        {
            return Psat_Pa(T_Cel, out _);
        }

       
        /// <summary>
        /// 40-100° 的孤点表格
        /// </summary>
        private static readonly (double T, double P)[] PsatTable_40_100 = new (double T, double P)[]
        {
            (40.0,  0.00654652772585),
            (45.0,  0.00857252606543),
            (50.0,  0.01111728162136),
            (55.0,  0.01428526656049),
            (60.0,  0.01819589403823),
            (65.0,  0.02298462869395),
            (70.0,  0.02880405354043),
            (75.0,  0.03582488206914),
            (80.0,  0.04423690594786),
            (85.0,  0.05424987039463),
            (90.0,  0.06609427109036),
            (95.0,  0.08002206826223),
            (100.0, 0.09630731526198),
        };
        

        /// <summary>
        /// 40°-100°的孤点计算
        /// </summary>
        /// <param name="T_Cel"></param>
        /// <returns></returns>
        private double InterpolatePsat_40_100(double T_Cel)
        {
            var pt = PsatTable_40_100;
            int last = pt.Length - 1;

            // 查找边界
            if (T_Cel <= pt[0].T)
            {
                return pt[0].P;
            }
            if (T_Cel >= pt[last].T)
            {
                return pt[last].P;
            }

            // 定位区间
            for(int i = 0; i < last;i++)
            {
                if (T_Cel >= pt[i].T && T_Cel <= pt[i + 1].T)
                {
                    double invT = 1.0 / (T_Cel + 273.15);
                    double invT0 = 1.0 / (pt[i].T + 273.15);
                    double invT1 = 1.0 / (pt[i + 1].T + 273.15);
                    double t = (invT - invT0) / (invT1 - invT0);
                    return Math.Exp(Math.Log(pt[i].P) + t * (Math.Log(pt[i + 1].P) - Math.Log(pt[i].P)));
                }
            }

            // 若未找到，返回最近点
            return (T_Cel < pt[0].T) ? pt[0].P : pt[last].P;

        }

        /// <summary>
        /// 计算饱和压力
        /// 部分孤立不收敛点采用邻域插值计算
        /// </summary>
        /// <param name="T_Cel">温度（℃）</param>
        /// <param name="status">状态（Converged:正常收敛）或（Interpolated:插值获得）</param>
        /// <returns>饱和压力（Pa）</returns>

        private double Psat_Pa(double T_Cel,out string status)
        {
            // =========== 40° - 100° ，线性插值============
            // 修正 2026-09-08 ：修复内层密度求解与迭代初值
            // 插值表兜底
            if(T_Cel>=40.0 && T_Cel <= 100.0)
            {
                try
                {
                    double Pdir = Ps_Pa(T_Cel);
                    if (IsPsatReliable(T_Cel, Pdir / 1E6))
                    {
                        status = "Converged 正常收敛";
                        return Pdir;
                    }
                }
                catch
                {
                    double Psat_MPa = InterpolatePsat_40_100(T_Cel);
                    status = "Interpolated(40-100) 兜底";
                    return Psat_MPa * 1E6;
                }
                               
            }
            // =========== 正式计算 ==============
            double P = double.NaN;
            bool needInterpolate = false; 
            try
            {
                P = Ps_Pa(T_Cel);
                double P_MPa = P / 1e6;

                // ----- 置信区间判断 ---------
                if (IsPsatReliable(T_Cel, P_MPa))
                {
                    //Console.WriteLine($"进入后备插值，t={T_Cel}℃");
                    status = "Converged 正常收敛";
                    return P;
                }
                else
                {
                    //Console.WriteLine($"警告：未进入后备插值，t={T_Cel}℃");
                    //Console.WriteLine($"数值结果不可信：t={T_Cel}℃，P={P_MPa}MPa，尝试插值");
                    needInterpolate = true;                             
                }
            }
            catch(Exception ex)
            {
                //日志
                //Console.WriteLine($"Ps_Pa failed for t={T_Cel}℃：{ex.Message},尝试插值");
            }

            // =============== 后备计算 ========================

            // 后备：在 ±2℃ 内寻找 可用点，若失败则 逐步扩大至 ±10 ℃
            if (needInterpolate)
            {                                   
                
                double[] offsets = { 2.0, 4.0, 6.0, 8.0, 10.0, 12.0, 14.0,16.0,18.0,20.0, };
                foreach (double dT in offsets)
                {
                    double leftT = T_Cel - dT;
                    double rightT = T_Cel + dT;
                    double? pLeft = TryGetPs(leftT);
                    double? pRight = TryGetPs(rightT);

                    // 校验临近点是否可信（单位转换为MPa）
                    bool leftOK = pLeft.HasValue && IsPsatReliable(leftT, pLeft.Value / 1e6);
                    bool rightOK = pRight.HasValue && IsPsatReliable(rightT, pRight.Value / 1e6);

                    //Console.WriteLine($"尝试偏移 {dT}℃: leftT={leftT}, pLeft={pLeft}, leftOK={leftOK}; rightT={rightT}, pRight={pRight}, rightOK={rightOK}");

                    if (leftOK && rightOK)
                    {
                        //线性内插
                        // 修正 2026-09-08：改为 （lnP ~ 1/t)内插

                        double pL = pLeft.Value / 1e6, pR = pRight.Value / 1e6;
                        double invT = 1.0 / (T_Cel + 273.15);
                        double invT0 = 1.0 / (leftT + 273.15);
                        double invT1 = 1.0 / (rightT + 273.15);
                        double t = (invT - invT0) / (invT1 - invT0);
                        double pInterp = Math.Exp(Math.Log(pL) + t * (Math.Log(pR) - Math.Log(pL)));

                        //double t = (T_Cel - leftT) / (rightT - leftT);
                        //double pInterp = pLeft.Value + t * (pRight.Value - pLeft.Value);                        
                        status = "Interpolated lnP~1/T内插";
                        return pInterp*1e6;
                    }
                    else if (leftOK)
                    {
                        status = "Interpolated 左侧值";
                        return pLeft.Value;
                    }
                    else if (rightOK)
                    {
                        status = "Interpolated 右侧值";
                        return pRight.Value;
                    }
                }
                throw new Exception($"Psat_Pa failed: cannot find valid neighbor for t={T_Cel}℃");
            }
            
            throw new Exception($"Psat_Pa failed: cannot find valid neighbor for t={T_Cel}℃");
        }
        /// <summary>
        /// 辅助函数，为Psat_Pa服务，评估Ps_Pa是否可以正常收敛，如果可以，直接用Ps_Pa数据，不能的话，采用内插数据。
        /// </summary>
        /// <param name="T_Cel"></param>
        /// <returns></returns>
        private double? TryGetPs(double T_Cel)
        {
            try
            {
                return Ps_Pa(T_Cel);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 辅助函数 独立结算结果可信数据判断
        /// </summary>
        /// <param name="T_Cel"></param>
        /// <param name="Psat_Pa"></param>
        /// <returns></returns>
        private bool IsPsatReliable(double T_Cel,double Psat_Pa)
        {
            //1. 压力为正
            if (Psat_Pa <= 0) return false;
            //2. 温度高于100℃，压力至少0.05MPa
            if (T_Cel > 100.0 && Psat_Pa < 0.05) return false;
            //3. 温度高于180℃，压力至少0.5MPa
            if (T_Cel > 180.0 && Psat_Pa < 0.1) return false;
            return true;
        }

        /// <summary>
        /// 饱和压力的 初值估计,不作为计算结果输出
        /// 形式:LnP=A-B/T
        /// </summary>
        /// <param name="T_K"></param>
        /// <returns></returns>
        private static double PsatEstimate_CC(double T_K)
        {
            const double Tt = 277.0, Pt = 6.630849e-4;
            const double Tb = 374.55, Pb = 0.101325;
            const double Tc2 = 643.847, Pc2 = 21.6618;

            double Ta, Pa, Tz, Pz;
            if (T_K < Tb) { Ta = Tt;Pa = Pt;Tz = Tb;Pz = Pb; }
            else { Ta = Tb;Pa = Pb;Tz = Tc2;Pz = Pc2; }

            double x1 = 1.0 / Ta, x2 = 1.0 / Tz;
            double y1 = Math.Log(Pa), y2 = Math.Log(Pz);
            double B = (y2 - y1) / (x1 - x2);
            double A = y1 + B * x1;
            return Math.Exp(A - B / T_K);

        }

        /// <summary>
        /// 计算重水饱和压力及两相密度（割线法，鲁棒版）
        /// </summary>
        private double Ps_Pa(double T_Cel)
        {
            double T_K = T_Cel + 273.15;
            
            if (T_K >= Tc_K)
                throw new Exception($"温度达到或超过临界温度");
            //if(T_K>254.415 && T_K <= 276.969)
            {
                //    throw new Exception($"D2O处于 ice Ih ,程序不适用");
            }

            if (T_K < 254.415)
                throw new Exception($"温度低于三相点");
            
            double R = R_J_per_mol_K;
            double Rhoc = Rhoc_mol_per_dm3;
            /*
             * 2026.7.28日 用新计算方法测试
             * 1. 50℃以下无解，200℃单点无解，60℃ - 351℃ 有解 
             * 2026.7.29 新增方法 解决 7.28的问题
             * 2026.7.30 新增内插法 解决 孤点计算 的 问题
             *           新增置信区间，内插解决 局部计算 结果偏离问题
             * 
             * 
             */

            // 残差函数 ：P_MPa - P_Chem_MPa

            Func<double, double> F = (P_MPa) =>
            {
                try
                {
                    double rhoV = Rho_Density_mol_per_dm3(T_Cel, P_MPa, "vapor",true);
                    double rhoL = Rho_Density_mol_per_dm3(T_Cel, P_MPa, "liquid",true);

                    //检查密度
                    // 修正 2026-09-08：Rho_Density 失败返回 NaN;

                    if(double.IsNaN(rhoV) || rhoV <= 0)
                    {
                        //气相不存在->压力高于饱和压力->残差为正
                        return +P_MPa;
                    }
                    if(double.IsNaN(rhoL)|| rhoL <= 0)
                    {
                        return -P_MPa;
                    }
                    /*
                    if (double.IsNaN(rhoV) || double.IsNaN(rhoL) || rhoV <= 0 || rhoL <= 0)
                    {
                        throw new Exception("密度错误");
                    }
                    */
                    double P_chem_Pa = Ps_from_Chem(rhoV, rhoL, T_K);
                    double P_chem_MPa = P_chem_Pa / 1e6;

                   
                    // 若P_chem 不合理，触发容错
                    if (double.IsNaN(P_chem_MPa) || double.IsInfinity(P_chem_MPa))
                    {
                        throw new Exception("化学势计算不合理");
                    }
                    //Console.Write($"T_Cel={T_Cel}℃,");
                    //Console.WriteLine($"rhoV={rhoV:F10}，rhoL={rhoL:F2},P_chem_MPa={P_chem_MPa:F10}，P_MPa={P_MPa:f8},F(P_MPa）={P_MPa-P_chem_MPa}");
                    return P_MPa - P_chem_MPa;
                }
                catch
                {
                    return -P_MPa;
                }

            };
          
            // 修正 2026-09-08： 

            double P_est = PsatEstimate_CC(T_K);

            double x0 = P_est * 0.3, x1 = Math.Min(P_est * 3.0, 30.0);
            double f0 = F(x0), f1 = F(x1);

            // 未异号，以几何方式扩张
            for(int i = 0; i < 80; i++)
            {
                if (f0 * f1 < 0) break;
                if (Math.Abs(f0) < Math.Abs(f1))
                {
                    x0 *= 0.3;
                    if (x0 < 1e-12) { x0 = 1e-12; }
                    f0 = F(x0);
                }
                else
                {
                    x1 *= 0.3;
                    if (x1 > 30.0) { x1 = 30.0; }
                    f1 = F(x1);
                }
                if (x0 <= 1e-12 && x1 >= 30.0) break;

            }

            if (f0 * f1 >= 0)
            {
                throw new Exception($"无法找到饱和压力区间，T={T_Cel}℃，F0={f0};F1={f1}");
            }


            /*
            // 初值选择
            // 2026.7.28 初值  
            double P1, P2;

            if(T_K <= 283.15)             //10
            {
                P1 = 1E-6;
                P2 = 0.005;
            }                     
            else if (T_K <= 323.15)       //<50
            {
                P1 = 1E-5;
                P2 = 0.012;
            }
            else if (T_K <= 333.15)       //<60
            {
                P1 = 1e-4;
                P2 = 0.019;
            }
            else if (T_K <= 343.15)            //<70
            {
                P1 = 1E-4;
                P2 = 0.03;
            }
            else if (T_K <= 353.15)                 //<80
            {
                P1 = 1e-4;
                P2 = 0.045;
            }
            else if(T_K <= 363.15)                       //<90
            {
                P1 = 1E-6;
                P2 = 0.09;
            }
            else if (T_K <= 373.15)                           //<100
            {
                P1 = 1e-6;
                P2 = 0.5;
            }

            else if (T_K <= 460.0)
            {
                P1 = 1E-6;
                P2 = 1.1999;
                
            }
            else
            {
                P1 = 1.0999;
                P2 = 22.0;
            }                                           
            
            double F1 = F(P1);
            double F2 = F(P2);
            
            // 确保初始两点异号
            for(int i = 0; i < 100; i++)
            {
                if (f1 * f2 < 0) break;
                if (Math.Abs(F1) < Math.Abs(F2))
                {
                    P1 *= 0.5;
                    if (P1 < 1e-12) P1 = 1e-12;
                    
                    F1 = F(P1);
                }
                else
                {
                    P2 *= 1.2;
                    if (P2 > 30.0) P2 = 30.0;
                    F2 = F(P2);
                }
                if (P1 < 1E-12 && P2 >= 30.0) break;
            }

            if (F1 * F2 >= 0)
            {
                throw new Exception($"无法找到饱和压力区间，T1={T_Cel}℃，F1={F1};F2={F2}");
            }
            
            // 割线法迭代
            double x0 = P1, x1 = P2;
            double f0 = F1, f1 = F2;
            */
            // 割线法
            for(int iter = 0; iter < 100; iter++)
            {
                
                if (Math.Abs(f1 - f0) < 1e-20) break;

                double x2 = x1 - f1 * (x1 - x0) / (f1 - f0);

                //限制幅度
                double lo = Math.Min(x0, x1), hi = Math.Max(x0, x1);
                if (!(x2 > lo && x2 < hi)) x2 = Math.Sqrt(x0 * x1);

               
                double f2 = F(x2);

                if(Math.Abs(f2)<=1e-12*Math.Max(x2,1e-9) || Math.Abs(x2 - x1) <= 1e-12 * Math.Max(x2, 1e-9))
                {
                    return x2 * 1e6;
                }

                if (f1 * f2 < 0)
                {
                    x0 = x1;f0 = f1;
                }
                else
                {
                    f0 *= 0.5;
                }
                x1 = x2;f1 = f2;
            }
            double res = ((x0 + x1) / 2.0) * 1e6;
            return res;
            


            /*
             *     2026.7.27日算法
             *     计算结果，显示低温状态发散，中高温相对合理
             * 
             * 
            // 初始密度估计（非常宽松）
            double rhoV = Math.Max(1e-8, 0.01 * 1e6 / (R * T_K) / 1000.0); // 理想气体近似
            double rhoL = Rhoc * 1.5;

            double omega = 0.7; // 阻尼系数，提高稳定性
            double Psat_Pa = 0;
            for (int iter = 0; iter < 200; iter++)
            {
                // 计算当前饱和压力
                Psat_Pa = Ps_from_Chem(rhoV, rhoL, T_K);
                double Psat_MPa = Psat_Pa / 1e6;

                // 健康检查：如果压力无效，重置密度并减小阻尼
                if (double.IsNaN(Psat_Pa) || double.IsInfinity(Psat_Pa) || Psat_Pa <= 1e-12)
                {
                    rhoV = Math.Max(1e-12, 0.001 / (R * T_K) / 1000.0); // 重置为 0.001 MPa 理想气体
                    rhoL = Rhoc * 1.5;
                    omega = Math.Max(0.1, omega * 0.5);
                    continue;
                }
                Psat_MPa = Psat_Pa / 1e6;
                if (Psat_MPa <= 1e-12) Psat_MPa = 1e-12;

                double rhoV_new = Rho_Density_mol_per_dm3(T_Cel, Psat_MPa, "vapor");
                double rhoL_new = Rho_Density_mol_per_dm3(T_Cel, Psat_MPa, "liquid");

                // 如果密度求解失败，同样重置
                if (double.IsNaN(rhoV_new) || double.IsNaN(rhoL_new) || rhoV_new <= 0 || rhoL_new <= 0)
                {
                    rhoV = Math.Max(1e-12, 0.001 / (R * T_K) / 1000.0);
                    rhoL = Rhoc * 1.5;
                    omega = Math.Max(0.1, omega * 0.5);
                    continue;
                }

                // 阻尼更新
                double rhoV_damped = omega * rhoV_new + (1 - omega) * rhoV;
                double rhoL_damped = omega * rhoL_new + (1 - omega) * rhoL;

                double errV = Math.Abs(rhoV_damped - rhoV) / Math.Max(rhoV_damped, 1e-14);
                double errL = Math.Abs(rhoL_damped - rhoL) / Math.Max(rhoL_damped, 1e-14);

                rhoV = rhoV_damped;
                rhoL = rhoL_damped;

                if (Math.Max(errV, errL) < 1e-10)
                    return Psat_Pa;
            
            }

            // 循环结束仍未收敛，返回最后有效值
            double finalP = Ps_from_Chem(rhoV, rhoL, T_K);
            if (double.IsNaN(finalP) || finalP <= 0)
                throw new Exception($"无法计算饱和压力，t={T_Cel}℃");
            return finalP;
            */
        }

        // =================================== 6. 偏微分解析函数 ======================
        // 6.1 理想部分 Table 4.
        private double alpha0(double tau,double delta)
        {
            if (_idealData == null) throw new Exception("idealData 未初始化");
            double a1 = _idealData.a1;
            double a2 = _idealData.a2;
            double c0 = _idealData.c0;
            double LnDelta = Math.Log(delta);
            double LnTau = Math.Log(tau);

            double part4 = 0;
            for(int i = 1; i <= 4; i++)
            {
                part4 += _idealData.vi[i] * Math.Log(1.0 - Math.Exp(-_idealData.ui[i] * tau / Tc_K));
            }
            double res = LnDelta + a1 + a2 * tau + (c0 - 1.0) * LnTau + part4;
            return res;
        }
        private double alpha0_delta(double delta)
        {
            return 1.0 / delta;
        }
        private double alpha0_delta_delta(double delta)
        {
            return -1.0 / delta / delta;
        }
        private double alpha0_tau(double tau)
        {
            double a2 = _idealData.a2;
            double c0 = _idealData.c0;
            
            double part4 = 0;
            for(int i = 1; i <= 4; i++)
            {
                double exp = Math.Exp(-_idealData.ui[i] * tau / Tc_K);
                part4 += _idealData.vi[i] * (_idealData.ui[i] / Tc_K) * (1.0 / (1.0 - exp) - 1.0);
            }
            double res = a2 + (c0 - 1.0) / tau + part4;
            return res;
        }
        private double alpha0_tau_tau(double tau)
        {
            double c0 = _idealData.c0;
            double part4 = 0;
            for(int i = 1; i <= 4; i++)
            {
                double exp = Math.Exp(-_idealData.ui[i] * tau / Tc_K);
                part4 += _idealData.vi[i] * (_idealData.ui[i] / Tc_K) * (_idealData.ui[i] / Tc_K) * (exp / ((1.0 - exp) * (1.0 - exp)));
            }
            double res = -1.0 * (c0 - 1.0) / tau / tau - part4;
            return res;
        }
        private double alpha0_delta_tau()
        {
            return 0;
        }
        // 6.2 剩余部分 Table 5.
        private double alphaR(double tau,double delta)
        {
            double part1 = 0;
            for(int i = 1; i <= 6; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni=_residualData.epsiloni[i];

                part1 += ni * Math.Pow(delta, di) * Math.Pow(tau, ti);
            }
            double part2 = 0;
            for(int i = 7; i <= 12; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                double exp = -Math.Pow(delta, li);

                part2 += ni * Math.Pow(delta, di) * Math.Pow(tau, ti) * Math.Exp(exp);

            }
            double part3 = 0;
            for (int i = 13; i <= 24; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];
                double exp = -etai * (delta - epsiloni) * (delta - epsiloni) - betai * (tau - gammai) * (tau - gammai);

                part3 += ni * Math.Pow(delta, di) * Math.Pow(tau, ti) * Math.Exp(exp);
            }
            return part1 + part2 + part3;
        }
        private double alphaR_delta(double tau,double delta)
        {
            double part1 = 0;
            for (int i = 1; i <= 6; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                part1 += ni * di * Math.Pow(delta, di - 1) * Math.Pow(tau, ti);
            }
            double part2 = 0;
            for (int i = 7; i <= 12; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                double exp = -Math.Pow(delta, li);

                part2 += ni * Math.Pow(delta, di - 1) * Math.Pow(tau, ti) * (di - li * Math.Pow(delta, li)) * Math.Exp(exp);

            }
            double part3 = 0;
            for (int i = 13; i <= 24; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];
                double exp = -etai * (delta - epsiloni) * (delta - epsiloni) - betai * (tau - gammai) * (tau - gammai);

                part3 += ni * Math.Pow(delta, di - 1) * Math.Pow(tau, ti) * Math.Exp(exp) * (di - 2 * etai * delta * (delta - epsiloni));
            }
            return part1 + part2 + part3;
        }
        private double alphaR_delta_delta(double tau,double delta)
        {
            double part1 = 0;
            for (int i = 1; i <= 6; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                part1 += ni * di * (di - 1) * Math.Pow(delta, di - 2) * Math.Pow(tau, ti);
            }
            double part2 = 0;
            for (int i = 7; i <= 12; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                double exp = -Math.Pow(delta, li);

                part2 += ni * Math.Pow(delta, di - 2) * Math.Pow(tau, ti) * 
                    ((di - li * Math.Pow(delta, li))*(di-1-li*Math.Pow(delta,li))-li*li*Math.Pow(delta,li)) 
                    * Math.Exp(exp);

            }
            double part3 = 0;
            for (int i = 13; i <= 24; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];
                double exp = -etai * (delta - epsiloni) * (delta - epsiloni) - betai * (tau - gammai) * (tau - gammai);

                part3 += ni * Math.Pow(tau, ti) * Math.Exp(exp)
                    * (
                    -2 * etai * Math.Pow(delta, di)
                    + 4 * etai * etai * Math.Pow(delta, di) * (delta - epsiloni) * (delta - epsiloni)
                    - 4 * di * etai * Math.Pow(delta, di - 1) * (delta - epsiloni)
                    + di * (di - 1) * Math.Pow(delta, di - 2));              
            }
            return part1 + part2 + part3;
        }
        private double alphaR_tau(double tau,double delta)
        {
            double part1 = 0;
            for (int i = 1; i <= 6; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                part1 += ni * ti * Math.Pow(delta, di) * Math.Pow(tau, ti-1);
            }
            double part2 = 0;
            for (int i = 7; i <= 12; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                double exp = -Math.Pow(delta, li);

                part2 += ni * ti* Math.Pow(delta, di) * Math.Pow(tau, ti - 1) * Math.Exp(exp);

            }
            double part3 = 0;
            for (int i = 13; i <= 24; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];
                double exp = -etai * (delta - epsiloni) * (delta - epsiloni) - betai * (tau - gammai) * (tau - gammai);

                part3 += ni * Math.Pow(delta, di) * Math.Pow(tau, ti) * Math.Exp(exp)
                    * (ti / tau - 2 * betai * (tau - gammai));
            }
            return part1 + part2 + part3;

        }
        private double alphaR_tau_tau(double tau,double delta)
        {

            double part1 = 0;
            for (int i = 1; i <= 6; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                part1 += ni * ti * (ti - 1) * Math.Pow(delta, di) * Math.Pow(tau, ti - 2);
            }
            double part2 = 0;
            for (int i = 7; i <= 12; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                double exp = -Math.Pow(delta, li);

                part2 += ni * ti * (ti - 1) * Math.Pow(delta, di) * Math.Pow(tau, ti - 2) * Math.Exp(exp);

            }
            double part3 = 0;
            for (int i = 13; i <= 24; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];
                double exp = -etai * (delta - epsiloni) * (delta - epsiloni) - betai * (tau - gammai) * (tau - gammai);
                double titau = ti / tau - 2 * betai * (tau - gammai);

                part3 += ni * Math.Pow(delta, di) * Math.Pow(tau, ti) * Math.Exp(exp)
                    * (titau * titau - ti / tau / tau - 2 * betai);
            }
            return part1 + part2 + part3;

        }
        private double alphaR_delta_tau(double tau,double delta)
        {
            double part1 = 0;
            for (int i = 1; i <= 6; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                part1 += ni * di * ti * Math.Pow(delta, di-1) * Math.Pow(tau, ti - 1);
            }
            double part2 = 0;
            for (int i = 7; i <= 12; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];

                double exp = -Math.Pow(delta, li);

                part2 += ni * ti * Math.Pow(delta, di - 1) * Math.Pow(tau, ti - 1) * (di - li * Math.Pow(delta, li)) * Math.Exp(exp);

            }
            double part3 = 0;
            for (int i = 13; i <= 24; i++)
            {
                double ni = _residualData.ni[i];
                int di = _residualData.di[i];
                double ti = _residualData.ti[i];
                int li = _residualData.li[i];
                double etai = _residualData.etai[i];
                double betai = _residualData.betai[i];
                double gammai = _residualData.gammai[i];
                double epsiloni = _residualData.epsiloni[i];
                double exp = -etai * (delta - epsiloni) * (delta - epsiloni) - betai * (tau - gammai) * (tau - gammai);
                double titau = ti / tau - 2 * betai * (tau - gammai);

                part3 += ni * Math.Pow(delta, di) * Math.Pow(tau, ti) * Math.Exp(exp)
                    * (di / delta - 2 * etai * (delta - epsiloni)) * titau;
            }
            return part1 + part2 + part3;
        }
    }
}


