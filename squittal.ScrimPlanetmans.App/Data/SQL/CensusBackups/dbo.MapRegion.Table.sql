USE [PlanetmansDbContext]
GO

SET NOCOUNT ON;

IF EXISTS ( SELECT *
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_NAME = N'MapRegion' )
BEGIN
    
  CREATE TABLE #Staging_MapRegion
    ( Id             int NOT NULL,
      FacilityId     int NOT NULL,
      FacilityName   nvarchar(max) NULL,
      FacilityTypeId int NOT NULL,
      FacilityType   nvarchar(max) null,
      ZoneId         int NOT NULL,
      IsDeprecated   bit NOT NULL,
      IsCurrent      bit NOT NULL );

  INSERT INTO #Staging_MapRegion VALUES
    (2101, 7500, N'Hvar', 4, N'Tech Plant', 2, 0, 1),
    (2102, 4401, N'Mao', 4, N'Tech Plant', 2, 0, 1),
    (2103, 4001, N'Allatum', 3, N'Bio Lab', 2, 0, 1),
    (2104, 3801, N'Saurva', 3, N'Bio Lab', 2, 0, 1),
    (2105, 3400, N'Peris', 2, N'Amp Station', 2, 0, 1),
    (2106, 3601, N'Rashnu', 3, N'Bio Lab', 2, 0, 1),
    (2107, 3201, N'Dahaka', 2, N'Amp Station', 2, 0, 1),
    (2108, 7000, N'Tawrich', 4, N'Tech Plant', 2, 0, 1),
    (2109, 118000, N'Zurvan', 2, N'Amp Station', 2, 0, 1),
    (2201, 7801, N'Indar Northern Warpgate', 7, N'Warpgate', 2, 0, 1),
    (2202, 120000, N'Indar Western Warpgate', 7, N'Warpgate', 2, 0, 1),
    (2203, 4801, N'Indar Eastern Warpgate', 7, N'Warpgate', 2, 0, 1),
    (2301, 5300, N'Camp Connery', 5, N'Large Outpost', 2, 0, 1),
    (2302, 5500, N'Camp Waterson', 5, N'Large Outpost', 2, 0, 1),
    (2303, 5100, N'Indar Excavation Site', 5, N'Large Outpost', 2, 0, 1),
    (2304, 5200, N'J908 Impact Site', 5, N'Large Outpost', 2, 0, 1),
    (2305, 5900, N'Crimson Bluff Tower', 5, N'Large Outpost', 2, 0, 1),
    (2306, 6200, N'The Crown', 5, N'Large Outpost', 2, 0, 1),
    (2307, 6100, N'Crossroads Watchtower', 5, N'Large Outpost', 2, 0, 1),
    (2308, 6000, N'Indar Bay Point', 5, N'Large Outpost', 2, 0, 1),
    (2309, 5800, N'Vanu Archives', 5, N'Large Outpost', 2, 0, 1),
    (2310, 5700, N'Regent Rock Garrison', 5, N'Large Outpost', 2, 0, 1),
    (2311, 6500, N'The Stronghold', 5, N'Large Outpost', 2, 0, 1),
    (2312, 6400, N'Feldspar Canyon Base', 5, N'Large Outpost', 2, 0, 1),
    (2313, 6300, N'Arroyo Torre Station', 5, N'Large Outpost', 2, 0, 1),
    (2402, 201, N'Indar Waste Treatment', 6, N'Small Outpost', 2, 0, 1),
    (2403, 202, N'NS Salvage Yard', 6, N'Small Outpost', 2, 1, 1),
    (2404, 203, N'Saurva Overflow Depot', 6, N'Small Outpost', 2, 0, 1),
    (2405, 204, N'Helios Solar, Inc.', 6, N'Small Outpost', 2, 0, 1),
    (2406, 205, N'Alkali Shipping', 6, N'Small Outpost', 2, 0, 1),
    (2407, 206, N'CoraMed Labs', 6, N'Small Outpost', 2, 0, 1),
    (2408, 207, N'Benson Construction', 6, N'Small Outpost', 2, 0, 1),
    (2409, 208, N'Alkali Mining Supply', 6, N'Small Outpost', 2, 0, 1),
    (2410, 209, N'Crater Firing Range', 6, N'Small Outpost', 2, 0, 1),
    (2411, 210, N'Alkali Storage', 6, N'Small Outpost', 2, 0, 1),
    (2412, 211, N'Briggs Laboratories', 6, N'Small Outpost', 2, 0, 1),
    (2413, 212, N'NS Refinery', 6, N'Small Outpost', 2, 0, 1),
    (2414, 213, N'Howling Pass Checkpoint', 5, N'Large Outpost', 2, 0, 1),
    (2415, 214, N'Indar Comm. Array', 6, N'Small Outpost', 2, 0, 1),
    (2416, 215, N'Quartz Ridge Camp', 5, N'Large Outpost', 2, 0, 1),
    (2417, 216, N'West Highlands Checkpoint', 6, N'Small Outpost', 2, 1, 1),
    (2418, 217, N'Seabed Listening Post', 6, N'Small Outpost', 2, 0, 1),
    (2419, 218, N'Ti Alloys, Inc.', 6, N'Small Outpost', 2, 0, 1),
    (2420, 219, N'Ceres Hydroponics', 6, N'Small Outpost', 2, 0, 1),
    (2421, 220, N'Galaxy Solar Plant', 6, N'Small Outpost', 2, 0, 1),
    (2422, 221, N'The Palisade', 6, N'Small Outpost', 2, 0, 1),
    (2423, 222, N'East Canyon Checkpoint', 6, N'Small Outpost', 2, 1, 1),
    (2424, 223, N'Abandoned NS Offices', 6, N'Small Outpost', 2, 1, 1),
    (2425, 224, N'NS Material Storage', 6, N'Small Outpost', 2, 0, 1),
    (2426, 225, N'Sandstone Gulch Mining', 6, N'Small Outpost', 2, 1, 1),
    (2427, 226, N'NS Secure Data Lab', 6, N'Small Outpost', 2, 0, 1),
    (2428, 227, N'Highlands Solar Station', 6, N'Small Outpost', 2, 0, 1),
    (2429, 228, N'Ayani Labs', 6, N'Small Outpost', 2, 0, 1),
    (2430, 229, N'Snake Ravine Lookout', 6, N'Small Outpost', 2, 0, 1),
    (2431, 230, N'XenoTech Labs', 6, N'Small Outpost', 2, 0, 1),
    (2432, 231, N'Broken Arch Road', 6, N'Small Outpost', 2, 1, 1),
    (2433, 232, N'Gravel Pass', 6, N'Small Outpost', 2, 0, 1),
    (2436, 235, N'Rust Mesa Lookout', 6, N'Small Outpost', 2, 0, 1),
    (2437, 236, N'The Old Stockpile', 6, N'Small Outpost', 2, 0, 1),
    (2438, 237, N'Ceres Biotech', 6, N'Small Outpost', 2, 0, 1),
    (2440, 239, N'Highlands Substation', 6, N'Small Outpost', 2, 0, 1),
    (2442, 241, N'NS Research Labs', 6, N'Small Outpost', 2, 1, 1),
    (2443, 242, N'Scarred Mesa Skydock', 6, N'Small Outpost', 2, 0, 1),
    (2444, 243, N'Red Ridge Communications', 6, N'Small Outpost', 2, 1, 1),
    (2447, 246, N'Copper Ravine Station', 6, N'Small Outpost', 2, 0, 1),
    (2448, 247, N'Old Auraxium Mines', 6, N'Small Outpost', 2, 0, 1),
    (2449, 248, N'Valley Storage Yard', 6, N'Small Outpost', 2, 0, 1),
    (2451, 250, N'Blackshard Iridium Mine', 6, N'Small Outpost', 2, 1, 1),
    (2453, 252, N'Ceres Farms', 6, N'Small Outpost', 2, 0, 1),
    (2454, 3410, N'Peris Barracks', 6, N'Small Outpost', 2, 0, 1),
    (2455, 3420, N'Peris Field Tower', 6, N'Small Outpost', 2, 0, 1),
    (2456, 3430, N'Peris Eastern Grove', 6, N'Small Outpost', 2, 0, 1),
    (2457, 4010, N'Allatum Research Lab', 6, N'Small Outpost', 2, 0, 1),
    (2458, 4020, N'Allatum Broadcast Hub', 6, N'Small Outpost', 2, 0, 1),
    (2459, 4030, N'Allatum Botany Wing', 6, N'Small Outpost', 2, 0, 1),
    (2460, 4430, N'Mao Southeast Gate', 6, N'Small Outpost', 2, 0, 1),
    (2461, 4420, N'Mao Watchtower', 6, N'Small Outpost', 2, 0, 1),
    (2462, 4410, N'Mao Southwest Gate', 6, N'Small Outpost', 2, 0, 1),
    (2463, 7020, N'Tawrich Tower', 6, N'Small Outpost', 2, 0, 1),
    (2464, 7030, N'Tawrich Recycling', 6, N'Small Outpost', 2, 0, 1),
    (2465, 7010, N'Tawrich Depot', 6, N'Small Outpost', 2, 0, 1),
    (2466, 3620, N'Rashnu Watchtower', 6, N'Small Outpost', 2, 0, 1),
    (2467, 3610, N'Rashnu Cavern', 6, N'Small Outpost', 2, 0, 1),
    (2468, 3630, N'Rashnu Southern Pass', 6, N'Small Outpost', 2, 0, 1),
    (2469, 118030, N'Zurvan Network Complex', 6, N'Small Outpost', 2, 0, 1),
    (2470, 118010, N'Zurvan Pump Station', 6, N'Small Outpost', 2, 0, 1),
    (2471, 118020, N'Zurvan Storage Yard', 6, N'Small Outpost', 2, 0, 1),
    (2472, 3810, N'Saurva Data Storage', 6, N'Small Outpost', 2, 0, 1),
    (2473, 3820, N'Saurva South Fortress', 6, N'Small Outpost', 2, 0, 1),
    (2474, 7520, N'Hvar Databank', 6, N'Small Outpost', 2, 0, 1),
    (2475, 7510, N'Hvar Western Post', 6, N'Small Outpost', 2, 0, 1),
    (2476, 7530, N'Hvar Physics Lab', 6, N'Small Outpost', 2, 0, 1),
    (2477, 3210, N'Dahaka Uplink Hub', 6, N'Small Outpost', 2, 0, 1),
    (2478, 3230, N'Dahaka Southern Post', 6, N'Small Outpost', 2, 0, 1),
    (2479, 3220, N'Dahaka Pump Station', 6, N'Small Outpost', 2, 0, 1),
    (4101, 261000, N'Sharpe''s Run', 6, N'Small Outpost', 4, 0, 1),
    (4102, 262000, N'Edgewater Overlook', 6, N'Small Outpost', 4, 0, 1),
    (4103, 263000, N'Last Hold', 6, N'Small Outpost', 4, 0, 1),
    (4104, 264000, N'Wainwright Armory', 6, N'Small Outpost', 4, 0, 1),
    (4105, 265000, N'The Offal Pit', 6, N'Small Outpost', 4, 0, 1),
    (4106, 266000, N'Kessel''s Crossing', 6, N'Small Outpost', 4, 0, 1),
    (4107, 267000, N'Hossin BioChem, Inc.', 6, N'Small Outpost', 4, 0, 1),
    (4108, 268000, N'VEX BioLogics', 6, N'Small Outpost', 4, 0, 1),
    (4109, 269000, N'Bunker J993', 6, N'Small Outpost', 4, 0, 1),
    (4110, 270000, N'Fallbridge Chemical', 6, N'Small Outpost', 4, 0, 1),
    (4111, 271000, N'Halcyon Watch', 6, N'Small Outpost', 4, 0, 1),
    (4112, 272000, N'Bridgewater Shipping Yard', 6, N'Small Outpost', 4, 0, 1),
    (4113, 273000, N'OMR Terraforming', 6, N'Small Outpost', 4, 0, 1),
    (4114, 274000, N'Iron Quay', 6, N'Small Outpost', 4, 0, 1),
    (4115, 275000, N'Broken Vale Garrison', 6, N'Small Outpost', 4, 0, 1),
    (4116, 276000, N'Woodman ASE Labs', 6, N'Small Outpost', 4, 0, 1),
    (4117, 277000, N'Johari Cove', 6, N'Small Outpost', 4, 0, 1),
    (4118, 278000, N'Roothouse Distillery', 6, N'Small Outpost', 4, 0, 1),
    (4119, 279000, N'Hunter''s Blind', 6, N'Small Outpost', 4, 0, 1),
    (4120, 280000, N'Gourney Dam', 6, N'Small Outpost', 4, 0, 1),
    (4121, 281000, N'SRP Hydroponics, Inc.', 6, N'Small Outpost', 4, 0, 1),
    (4122, 282000, N'The Ziggurat', 6, N'Small Outpost', 4, 0, 1),
    (4123, 283000, N'Nettlemire Gardens', 6, N'Small Outpost', 4, 0, 1),
    (4124, 284000, N'Whispering Pass', 6, N'Small Outpost', 4, 0, 1),
    (4125, 285000, N'SRP Nanite Relay Station', 6, N'Small Outpost', 4, 0, 1),
    (4126, 286000, N'Four Fingers', 6, N'Small Outpost', 4, 0, 1),
    (4127, 287000, N'Kestral Watch', 6, N'Small Outpost', 4, 0, 1),
    (4130, 289000, N'Genudine Holographics', 5, N'Large Outpost', 4, 0, 1),
    (4131, 290000, N'Fort Drexler', 5, N'Large Outpost', 4, 0, 1),
    (4132, 291000, N'Cairn Station', 5, N'Large Outpost', 4, 0, 1),
    (4133, 292000, N'Southgate Checkpoint', 5, N'Large Outpost', 4, 0, 1),
    (4134, 293000, N'Genesis Terraforming Plant', 5, N'Large Outpost', 4, 0, 1),
    (4135, 294000, N'Bravata PMC Compound', 5, N'Large Outpost', 4, 0, 1),
    (4136, 295000, N'Matsuda Genetics', 5, N'Large Outpost', 4, 0, 1),
    (4137, 296000, N'Hayd Skydock', 5, N'Large Outpost', 4, 0, 1),
    (4138, 297000, N'Hatcher Airstation', 5, N'Large Outpost', 4, 0, 1),
    (4139, 298000, N'Nason''s Defiance', 5, N'Large Outpost', 4, 0, 1),
    (4140, 299000, N'Naum', 2, N'Amp Station', 4, 0, 1),
    (4141, 299010, N'Naum Ravine Overpass', 6, N'Small Outpost', 4, 0, 1),
    (4142, 299020, N'Naum Marsh Compound', 6, N'Small Outpost', 4, 0, 1),
    (4143, 299030, N'Naum Forward Barracks', 6, N'Small Outpost', 4, 0, 1),
    (4150, 300000, N'Hurakan', 2, N'Amp Station', 4, 0, 1),
    (4151, 300010, N'Hurakan Secure Storage', 6, N'Small Outpost', 4, 0, 1),
    (4152, 300020, N'Hurakan Western Pass', 6, N'Small Outpost', 4, 0, 1),
    (4153, 300030, N'Hurakan Southern Depot', 6, N'Small Outpost', 4, 0, 1),
    (4160, 301000, N'Ixtab', 2, N'Amp Station', 4, 0, 1),
    (4161, 301010, N'Ixtab Power Regulation', 6, N'Small Outpost', 4, 0, 1),
    (4162, 301020, N'Ixtab Water Purification', 6, N'Small Outpost', 4, 0, 1),
    (4163, 301030, N'Ixtab Southern Pass', 6, N'Small Outpost', 4, 0, 1),
    (4170, 302000, N'Acan', 3, N'Bio Lab', 4, 0, 1),
    (4171, 302010, N'East Acan Storage Depot', 6, N'Small Outpost', 4, 0, 1),
    (4172, 302020, N'Acan Data Hub', 6, N'Small Outpost', 4, 0, 1),
    (4173, 302030, N'Acan Southern Labs', 6, N'Small Outpost', 4, 0, 1),
    (4180, 303000, N'Bitol', 3, N'Bio Lab', 4, 0, 1),
    (4181, 303010, N'Bitol Stockpile', 6, N'Small Outpost', 4, 0, 1),
    (4182, 303020, N'Bitol Northern Outpost', 6, N'Small Outpost', 4, 0, 1),
    (4183, 303030, N'Bitol Eastern Depot', 6, N'Small Outpost', 4, 0, 1),
    (4190, 304000, N'Zotz', 3, N'Bio Lab', 4, 0, 1),
    (4191, 304010, N'Zotz North Garden', 6, N'Small Outpost', 4, 0, 1),
    (4192, 304020, N'Zotz Arboretum', 6, N'Small Outpost', 4, 0, 1),
    (4193, 304030, N'Zotz Agriculture Lab', 6, N'Small Outpost', 4, 0, 1),
    (4200, 305000, N'Ghanan', 4, N'Tech Plant', 4, 0, 1),
    (4201, 305010, N'Ghanan Southern Crossing', 6, N'Small Outpost', 4, 0, 1),
    (4202, 305020, N'Ghanan Research Labs', 6, N'Small Outpost', 4, 0, 1),
    (4203, 305030, N'Ghanan Eastern Gatehouse', 6, N'Small Outpost', 4, 0, 1),
    (4210, 306000, N'Mulac', 4, N'Tech Plant', 4, 0, 1),
    (4211, 306010, N'Mulac Pass', 6, N'Small Outpost', 4, 0, 1),
    (4212, 306020, N'Mulac Foundry', 6, N'Small Outpost', 4, 0, 1),
    (4213, 306030, N'Mulac Purification Site', 6, N'Small Outpost', 4, 0, 1),
    (4220, 307000, N'Chac', 4, N'Tech Plant', 4, 0, 1),
    (4221, 307010, N'Chac Fusion Lab', 6, N'Small Outpost', 4, 0, 1),
    (4222, 307020, N'Chac Water Purification', 6, N'Small Outpost', 4, 0, 1),
    (4223, 307030, N'Chac Intel Hub', 6, N'Small Outpost', 4, 0, 1),
    (4230, 308000, N'Hossin Western Warpgate', 7, N'Warpgate', 4, 0, 1),
    (4240, 309000, N'Hossin Eastern Warpgate', 7, N'Warpgate', 4, 0, 1),
    (4250, 310000, N'Hossin Southern Warpgate', 7, N'Warpgate', 4, 0, 1),
    (4260, 287010, N'Construction Site Alpha', 6, N'Small Outpost', 4, 0, 1),
    (4261, 287020, N'Construction Site Beta', 6, N'Small Outpost', 4, 0, 1),
    (4262, 287030, N'Construction Site Gamma', 6, N'Small Outpost', 4, 0, 1),
    (4263, 287040, N'Takkon Storage', 6, N'Small Outpost', 4, 0, 1),
    (4264, 287050, N'Construction Site Zeta', 6, N'Small Outpost', 4, 0, 1),
    (4265, 287060, N'Eastern Substation', 6, N'Small Outpost', 4, 0, 1),
    (4266, 287070, N'Fort Liberty', 6, N'Small Outpost', 4, 0, 1),
    (4267, 287080, N'Construction Site Epsilon', 6, N'Small Outpost', 4, 0, 1),
    (4268, 287090, N'Mossridge Command Center', 6, N'Small Outpost', 4, 0, 1),
    (4269, 287100, N'Construction Site Omega', 6, N'Small Outpost', 4, 0, 1),
    (4270, 287110, N'Outpost Lambda', 6, N'Small Outpost', 4, 0, 1),
    (4271, 287120, N'Construction Site Sigma', 6, N'Small Outpost', 4, 0, 1),
    (6001, 200000, N'Amerish Western Warpgate', 7, N'Warpgate', 6, 0, 1),
    (6002, 201000, N'Amerish Eastern Warpgate', 7, N'Warpgate', 6, 0, 1),
    (6003, 203000, N'Amerish Southern Warpgate', 7, N'Warpgate', 6, 0, 1),
    (6101, 204000, N'Kwahtee', 2, N'Amp Station', 6, 0, 1),
    (6102, 205000, N'Ikanam', 3, N'Bio Lab', 6, 0, 1),
    (6103, 206000, N'Heyoka', 4, N'Tech Plant', 6, 0, 1),
    (6111, 207000, N'Sungrey', 2, N'Amp Station', 6, 0, 1),
    (6112, 208000, N'Mekala', 4, N'Tech Plant', 6, 0, 1),
    (6113, 209000, N'Onatha', 3, N'Bio Lab', 6, 0, 1),
    (6121, 210000, N'Wokuk', 2, N'Amp Station', 6, 0, 1),
    (6122, 211000, N'Tumas', 4, N'Tech Plant', 6, 0, 1),
    (6123, 212000, N'Xelas', 3, N'Bio Lab', 6, 0, 1),
    (6201, 213000, N'LithCorp Secure Mine', 5, N'Large Outpost', 6, 0, 1),
    (6202, 214000, N'The NC Arsenal', 5, N'Large Outpost', 6, 0, 1),
    (6203, 215000, N'Crux Mining Operation', 5, N'Large Outpost', 6, 0, 1),
    (6204, 216000, N'Crux Headquarters', 5, N'Large Outpost', 6, 0, 1),
    (6205, 217000, N'The Bastion', 5, N'Large Outpost', 6, 0, 1),
    (6206, 218000, N'Auraxis Firearms Corp.', 5, N'Large Outpost', 6, 0, 1),
    (6207, 219000, N'Splitpeak Pass', 5, N'Large Outpost', 6, 0, 1),
    (6208, 220000, N'West Pass Watchtower', 5, N'Large Outpost', 6, 0, 1),
    (6209, 221000, N'Granite Valley Garrison', 5, N'Large Outpost', 6, 0, 1),
    (6301, 222000, N'Stoneridge Reserve', 6, N'Small Outpost', 6, 0, 1),
    (6302, 222010, N'North Grove Post', 6, N'Small Outpost', 6, 0, 1),
    (6303, 222020, N'Jagged Lance Mine', 6, N'Small Outpost', 6, 0, 1),
    (6304, 222030, N'Hidden Ridge Mining', 6, N'Small Outpost', 6, 0, 1),
    (6305, 222040, N'SolTech Charging Station', 6, N'Small Outpost', 6, 0, 1),
    (6306, 222050, N'Deserted Mineshaft', 6, N'Small Outpost', 6, 0, 1),
    (6307, 222060, N'AuraxiCom Network Hub', 6, N'Small Outpost', 6, 0, 1),
    (6308, 260004, N'Amerish ARX Reserve', 6, N'Small Outpost', 6, 0, 1),
    (6309, 222080, N'Genudine Physics Lab', 6, N'Small Outpost', 6, 0, 1),
    (6310, 222090, N'Cobalt Communications', 6, N'Small Outpost', 6, 0, 1),
    (6311, 222100, N'West Foothills Airdock', 6, N'Small Outpost', 6, 0, 1),
    (6312, 222110, N'DeepCore Geolab', 6, N'Small Outpost', 6, 0, 1),
    (6313, 222120, N'Blackshard Tungsten Mine', 6, N'Small Outpost', 6, 0, 1),
    (6314, 222130, N'Shadespire Farms', 6, N'Small Outpost', 6, 0, 1),
    (6316, 222150, N'Rockslide Outlook', 6, N'Small Outpost', 6, 0, 1),
    (6317, 222160, N'East Hills Checkpoint', 6, N'Small Outpost', 6, 0, 1),
    (6318, 222170, N'Silver Valley Arsenal', 6, N'Small Outpost', 6, 0, 1),
    (6319, 222180, N'Chimney Rock Depot', 6, N'Small Outpost', 6, 0, 1),
    (6320, 222190, N'AuraxiCom Substation', 6, N'Small Outpost', 6, 0, 1),
    (6323, 222220, N'Torremar Storage Yard', 6, N'Small Outpost', 6, 0, 1),
    (6324, 222230, N'Highroads Station', 6, N'Small Outpost', 6, 0, 1),
    (6325, 222240, N'Aramax Chemical Co.', 6, N'Small Outpost', 6, 0, 1),
    (6326, 222250, N'The Auraxian Cryobank', 6, N'Small Outpost', 6, 0, 1),
    (6328, 222270, N'Raven Landing', 6, N'Small Outpost', 6, 0, 1),
    (6329, 222280, N'The Ascent', 5, N'Large Outpost', 6, 0, 1),
    (6330, 222300, N'LithCorp Fortress', 6, N'Small Outpost', 6, 0, 1),
    (6331, 222310, N'Moss Ravine', 6, N'Small Outpost', 6, 0, 1),
    (6332, 222320, N'The Auger', 6, N'Small Outpost', 6, 0, 1),
    (6333, 222330, N'Shrouded Skyway', 6, N'Small Outpost', 6, 0, 1),
    (6334, 222340, N'The Scarfield Reliquary', 6, N'Small Outpost', 6, 0, 1),
    (6335, 222350, N'LithCorp Central', 6, N'Small Outpost', 6, 0, 1),
    (6336, 222360, N'Eastshore Training Camp', 6, N'Small Outpost', 6, 0, 1),
    (6337, 400128, N'Solus Nature Annex', 9, N'Construction Outpost', 6, 0, 1),
    (6338, 222380, N'Barrik Electrical Station', 6, N'Small Outpost', 6, 0, 1),
    (6339, 222290, N'SolTech Gorge', 6, N'Small Outpost', 6, 0, 1),
    (6340, 204001, N'Kwahtee West Pass', 6, N'Small Outpost', 6, 0, 1),
    (6341, 204002, N'Kwahtee Fortress', 6, N'Small Outpost', 6, 0, 1),
    (6342, 204003, N'Kwahtee Mountain Complex', 6, N'Small Outpost', 6, 0, 1),
    (6343, 205001, N'Ikanam Motor Pool', 6, N'Small Outpost', 6, 0, 1),
    (6344, 205002, N'Ikanam Garrison', 6, N'Small Outpost', 6, 0, 1),
    (6345, 205003, N'Ikanam Triage Station', 6, N'Small Outpost', 6, 0, 1),
    (6346, 206001, N'Heyoka Armory', 6, N'Small Outpost', 6, 0, 1),
    (6347, 206002, N'Heyoka Chemical Lab', 6, N'Small Outpost', 6, 0, 1),
    (6348, 207001, N'Sungrey West Gate', 6, N'Small Outpost', 6, 0, 1),
    (6349, 207002, N'Sungrey Power Hub', 6, N'Small Outpost', 6, 0, 1),
    (6350, 207003, N'Sungrey Overwatch', 6, N'Small Outpost', 6, 0, 1),
    (6351, 208001, N'Mekala Cart Mining', 6, N'Small Outpost', 6, 0, 1),
    (6352, 208002, N'Mekala''s Auxiliary Compound', 6, N'Small Outpost', 6, 0, 1),
    (6353, 209001, N'Onatha North Gate', 6, N'Small Outpost', 6, 0, 1),
    (6354, 209002, N'East Onatha Comm. Array', 6, N'Small Outpost', 6, 0, 1),
    (6355, 209003, N'Onatha Southwest Gate', 6, N'Small Outpost', 6, 0, 1),
    (6356, 210001, N'Wokuk Ecological Preserve', 6, N'Small Outpost', 6, 0, 1),
    (6357, 210002, N'Wokuk Shipping Dock', 6, N'Small Outpost', 6, 0, 1),
    (6358, 210003, N'Wokuk Watchtower', 6, N'Small Outpost', 6, 0, 1),
    (6359, 211001, N'Tumas Skylance Battery', 6, N'Small Outpost', 6, 0, 1),
    (6360, 211002, N'Tumas Cargo Facility', 6, N'Small Outpost', 6, 0, 1),
    (6361, 212001, N'Xelas West Air Dock', 6, N'Small Outpost', 6, 0, 1),
    (6362, 212002, N'Xelas North Gate', 6, N'Small Outpost', 6, 0, 1),
    (6363, 212003, N'Xelas South Bridge', 6, N'Small Outpost', 6, 0, 1),
    (18001, 230000, N'Palos Solar Array', 6, N'Small Outpost', 8, 0, 1),
    (18002, 231000, N'Crystal Ridge Comm Array', 6, N'Small Outpost', 8, 0, 1),
    (18003, 232000, N'Haven Outpost', 6, N'Small Outpost', 8, 0, 1),
    (18004, 233000, N'Aurora Materials Lab', 6, N'Small Outpost', 8, 0, 1),
    (18005, 234000, N'Apex Genetics', 6, N'Small Outpost', 8, 0, 1),
    (18006, 235000, N'Stillwater Watch', 6, N'Small Outpost', 8, 1, 1),
    (18007, 236000, N'Grey Heron Shipping', 5, N'Large Outpost', 8, 0, 1),
    (18008, 237000, N'The Traverse', 6, N'Small Outpost', 8, 0, 1),
    (18009, 250000, N'Jaeger''s Crossing', 5, N'Large Outpost', 8, 1, 1),
    (18010, 239000, N'Pale Canyon Chemical', 6, N'Small Outpost', 8, 0, 1),
    (18011, 240000, N'Old Shore Checkpoint', 6, N'Small Outpost', 8, 0, 1),
    (18012, 241000, N'Everett Supply Co.', 6, N'Small Outpost', 8, 1, 1),
    (18013, 242000, N'Eastwake Harborage', 6, N'Small Outpost', 8, 0, 1),
    (18014, 243000, N'Elli Forest Pass', 6, N'Small Outpost', 8, 0, 1),
    (18015, 244000, N'Frostfall Overlook', 6, N'Small Outpost', 8, 0, 1),
    (18016, 245000, N'Snowshear Fort', 5, N'Large Outpost', 8, 0, 1),
    (18017, 246000, N'Mattherson''s Triumph', 5, N'Large Outpost', 8, 0, 1),
    (18018, 247000, N'Northpoint Station', 5, N'Large Outpost', 8, 1, 1),
    (18019, 248000, N'Saerro Listening Post', 5, N'Large Outpost', 8, 0, 1),
    (18020, 249000, N'Waterson''s Redemption', 5, N'Large Outpost', 8, 0, 1),
    (18021, 238000, N'The Bulwark', 6, N'Small Outpost', 8, 1, 1),
    (18022, 251000, N'Andvari', 3, N'Bio Lab', 8, 1, 0),
    (18022, 400311, N'Andvari Ruins', 9, N'Construction Outpost', 8, 0, 1),
    (18023, 252000, N'Elli', 2, N'Amp Station', 8, 1, 1),
    (18024, 253000, N'Freyr', 2, N'Amp Station', 8, 0, 1),
    (18025, 254000, N'Eisa', 4, N'Tech Plant', 8, 0, 1),
    (18026, 255000, N'Mani', 3, N'Bio Lab', 8, 1, 1),
    (18027, 256000, N'Nott Communications', 6, N'Small Outpost', 8, 0, 1),
    (18028, 257000, N'Ymir', 9, N'Construction Outpost', 8, 0, 1),
    (18029, 258000, N'Esamir Northern Warpgate', 7, N'Warpgate', 8, 0, 1),
    (18030, 259000, N'Esamir Southern Warpgate', 7, N'Warpgate', 8, 0, 1),
    (18031, 260000, N'Esamir Eastern Warpgate', 7, N'Warpgate', 8, 1, 0),
    (18032, 244100, N'Echo Valley Substation', 6, N'Small Outpost', 8, 0, 1),
    (18033, 244200, N'Bridge-ward', 6, N'Small Outpost', 8, 0, 1),
    (18034, 244300, N'Frostbite Harbor', 6, N'Small Outpost', 8, 0, 1),
    (18035, 310005, N'East River Sky Station', 6, N'Small Outpost', 8, 1, 1),
    (18036, 244500, N'Jaeger''s Fist', 6, N'Small Outpost', 8, 0, 1),
    (18037, 244600, N'Two Stone Beach', 6, N'Small Outpost', 8, 0, 1),
    (18038, 260010, N'Terran BL-4 Crash Site', 5, N'Large Outpost', 8, 0, 1),
    (18046, 251010, N'Andvari Barracks', 6, N'Small Outpost', 8, 0, 1),
    (18047, 251020, N'Andvari Frozen Reservoir', 6, N'Small Outpost', 8, 1, 1),
    (18048, 251030, N'Andvari South Bank', 6, N'Small Outpost', 8, 0, 1),
    (18049, 252010, N'Elli Tower', 6, N'Small Outpost', 8, 0, 1),
    (18050, 252020, N'Elli Barracks Complex', 6, N'Small Outpost', 8, 1, 1),
    (18051, 253010, N'Freyr Northern Barracks', 6, N'Small Outpost', 8, 1, 1),
    (18052, 253020, N'Freyr Geothermal Plant', 6, N'Small Outpost', 8, 1, 1),
    (18053, 253030, N'Freyr Network Compound', 6, N'Small Outpost', 8, 1, 1),
    (18054, 253040, N'Freyr Substation', 6, N'Small Outpost', 8, 1, 1),
    (18055, 254010, N'Eisa Mountain Pass', 6, N'Small Outpost', 8, 1, 1),
    (18056, 254020, N'Eisa Southern Camp', 6, N'Small Outpost', 8, 1, 1),
    (18057, 254030, N'Eisa Mining Operation', 6, N'Small Outpost', 8, 1, 1),
    (18058, 255010, N'Mani Processing Plant', 6, N'Small Outpost', 8, 0, 1),
    (18059, 255020, N'Mani Fortress', 6, N'Small Outpost', 8, 1, 1),
    (18060, 255030, N'Mani Lake Satellite', 6, N'Small Outpost', 8, 0, 1),
    (18061, 256010, N'Nott Substation', 6, N'Small Outpost', 8, 1, 1),
    (18062, 256020, N'Nott Research Camp', 6, N'Small Outpost', 8, 1, 0),
    (18062, 260000, N'Esamir Eastern Warpgate', 7, N'Warpgate', 8, 0, 1),
    (18063, 256030, N'Genudine Gardens', 6, N'Small Outpost', 8, 0, 1),
    (18064, 257010, N'Ymir Mine Watch', 6, N'Small Outpost', 8, 1, 1),
    (18065, 257020, N'Ymir Eastern Way Station', 6, N'Small Outpost', 8, 1, 1),
    (18066, 257030, N'Ymir Southern Reach', 6, N'Small Outpost', 8, 1, 1),
    (18067, 244610, N'Rime Analytics', 6, N'Small Outpost', 8, 0, 1),
    (18068, 244620, N'The Rink', 6, N'Small Outpost', 8, 0, 1),
    (18204, 400129, N'Cobalt Geological Outpost', 9, N'Construction Outpost', 6, 0, 1),
    (18205, 400130, N'Berjess Overlook', 9, N'Construction Outpost', 2, 0, 1),
    (18206, 400131, N'Sunken Relay Station', 9, N'Construction Outpost', 2, 0, 1),
    (18207, 400132, N'Lowland Trading Post', 9, N'Construction Outpost', 2, 0, 1),
    (18208, 400133, N'BL-4 Recovery Point', 9, N'Construction Outpost', 8, 0, 1),
    (18209, 400134, N'Tapp Waystation', 9, N'Construction Outpost', 8, 0, 1),
    (18210, 400135, N'Untapped Reservoir', 9, N'Construction Outpost', 8, 0, 1),
    (18249, 0, N'The Wash', 0, NULL, 8, 0, 1),
    (18250, 400314, N'Baldur', 2, N'Amp Station', 8, 0, 1),
    (18251, 400315, N'Vidar Observation Site', 9, N'Construction Outpost', 8, 0, 1),
    (18252, 400316, N'Excavion DS-01E', 5, N'Large Outpost', 8, 0, 1),
    (18253, 400317, N'Jord', 2, N'Amp Station', 8, 0, 1);

  MERGE [dbo].[MapRegion] as target
    USING ( SELECT Id, FacilityId, FacilityName, FacilityTypeId, FacilityType, ZoneId, IsDeprecated, IsCurrent FROM #Staging_MapRegion ) as source
      ON ( target.Id = source.Id AND target.FacilityId = source.FacilityId )
    WHEN MATCHED THEN
      UPDATE SET FacilityId = source.FacilityId,
                 FacilityName = source.FacilityName,
                 FacilityTypeId = source.FacilityTypeId,
                 FacilityType = source.FacilityType,
                 ZoneId = source.ZoneId,
                 IsDeprecated = source.IsDeprecated,
                 IsCurrent = source.IsCurrent
    WHEN NOT MATCHED THEN
      INSERT ( [Id], [FacilityId], [FacilityName], [FacilityTypeId], [FacilityType], [ZoneId], [IsDeprecated], [IsCurrent] )
      VALUES ( source.Id, source.FacilityId, source.FacilityName, source.FacilityTypeId, source.FacilityType, source.ZoneId, source.IsDeprecated, source.IsCurrent );
  
  DROP TABLE #Staging_MapRegion;
    
END;