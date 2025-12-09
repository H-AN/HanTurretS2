<div align="center"><h1><img width="600" height="131" alt="68747470733a2f2f70616e2e73616d7979632e6465762f732f56596d4d5845" src="https://github.com/user-attachments/assets/d0316faa-c2d0-478f-a642-1e3c3651f1d4" /></h1></div>

<div class="section">
<div align="center"><h1>Turret for Swiftly2</h1></div>


<div align="center"><strong>基于 Swiftly2 框架开发的 CS2 自动机枪炮塔插件。</p></div>

<div align="center"><strong>支持多种自定义配置。</p></div>
<div align="center"><strong>支持自定义模型,伤害,攻击频率,范围,次数限制,金钱限制,管理员权限,自定义特效等。</p></div>
</div>

<div align="center">

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z31PY52N)
  

</div>

---

📦 创意工坊示例（炮塔 模型/音效等）


插件可结合以下创意工坊资源使用（示例）：
3618032051
```
要使用创意工坊资源,需要服务器安装metamod插件 multiaddonmanager 来管理服务器和玩家使用下载和安装创意工坊资源

安装multiaddonmanager插件后 在game\csgo\cfg\multiaddonmanager\multiaddonmanager.cfg配置文件中
 
找到第一行 mm_extra_addons  "3618032051"

把资源ID填写上去 等待服务器下载资源完毕 玩家进服会自动下载资源

之后用 Source2Viewer 软件 打开资源包 查看资源内的 模型路径与soundevent名字

之后根据需要填写到炮塔配置内使用
```
---

🧩 插件功能特色

支持 多种炮塔种类配置,多语言支持

支持 菜单选择炮塔创建  

支持自定义炮架与物理模型 不填写则应用上述创意工坊例子内模型 

自定义菜单开启命令 (默认: sw_turret) 可进入配置内自己设置指令 用sw_ 开头

可自定义炮塔的各种属性,多重个性化选项

唯一名称(Name)

开关 (Enable) 模型(Model) 索敌范围(Range)

攻击频率 Rate  (0.1 = 每0.1秒攻击一次)

自定义伤害 Damage 击退 KnockBack 攻击动画  FireAnim

队伍限制 Team (填写all 菜单所有队伍可见 填写ct只有ct能看到菜单内属于自己的炮塔)

价格 Price (填写 0 为免费 否则需要金钱购买)

限制 Limit (填写0为无限制创建,否则有创建限制)

管理员权限 Permissions (留空为不需要权限,否则需要权限才能创建)

透视外发光 GlowColor (留空为不设置外发光,否则根据rgba值设置外发光)

镭射光效 laserColor (留空为不设置镭射光束,否则根据rgba值来设置镭射光束)

炮塔音效 TurretFireSound (填写soundevent 来播放炮塔攻击音效)

炮塔枪口粒子 MuzzleParticle (自带预缓存)

枪口附件 MuzzleAttachment (需要自行寻找自己炮塔模型的附件来附加粒子) 

多个附件用 ,隔开 只有一个附件就只填写一个 插件会根据附件数量设置粒子

预缓存声音事件 PrecacheSoundEvent (填写声音事件文件可以用于预缓存)

多种炮塔属性可自由扩展
---

🧱 配置示例（节选）可以自由设置不同炮塔属性
```
{
  "HanTurretS2CFG": {
    "TurretList": [
		{
			"Enable": true,
			"Name": "炮塔CT(level1)",
			"Model": "models/stk_sentry_guns/sentry/lvl_1_ct.vmdl",
			"Range": 300,
			"Rate": 0.5,
			"Damage": 5,
			"KnockBack": 1000,
			"FireAnim": "fire",
			"Team": "ct",
			"Price": "0",
			"Limit": 0,
			"Permissions": "",
			"GlowColor": "0,0,255,255",
			"laserColor": "0,0,255,255",
			"TurretFireSound": "n4a_csdm_sentry.sentry_shoot",
			"PrecacheSoundEvent": "soundevents/n4a_csdm_sentry.vsndevts",
			"MuzzleParticle": "particles/stk_sentryguns/sparks_muzzle_core.vpcf",
			"MuzzleAttachment": "fire_pos_1"
		},
		{
			"Enable": true,
			"Name": "炮塔CT(level2)",
			"Model": "models/stk_sentry_guns/sentry/lvl_2_ct.vmdl",
			"Range": 500,
			"Rate": 0.4,
			"Damage": 20,
			"KnockBack": 1005,
			"FireAnim": "fire",
			"Team": "ct",
			"Price": "500",
			"Limit": 1,
			"Permissions": "",
			"GlowColor": "0,125,255,255",
			"laserColor": "0,125,255,255",
			"TurretFireSound": "n4a_csdm_sentry.sentry_shoot",
			"PrecacheSoundEvent": "soundevents/n4a_csdm_sentry.vsndevts",
			"MuzzleParticle": "particles/stk_sentryguns/sparks_muzzle_core.vpcf",
			"MuzzleAttachment": "fire_pos_1,fire_pos_2"
		}
	]
  }
}
```
---
CS2 Automated Machine Gun Turret Plugin (Based on Swiftly2 Framework)
This plugin introduces an automated machine gun turret feature for CS2, developed using the Swiftly2 framework.

It supports extensive custom configuration options. It allows for customization of the model, damage, fire rate, range, usage limits, money restrictions, admin permissions, custom effects, and more.

---

📦 Workshop Resource Example (Turret Model/Sound Effects, etc.)
The plugin can be used in conjunction with the following Workshop resources (example): Resource ID: 3618032051
```
To use Workshop resources, your server must have the 'metamod' plugin 'multiaddonmanager' installed to manage the downloading and installation of Workshop resources for the server and players.

After installing the 'multiaddonmanager' plugin, navigate to the configuration file:
game\csgo\cfg\multiaddonmanager\multiaddonmanager.cfg
 
Find the first line: mm_extra_addons  "3618032051"

Enter the Resource ID here and wait for the server to download the resource. Players joining the server will automatically download the resources.

Subsequently, use the **Source2Viewer** software to open the resource package and inspect the **model paths** and **sound event names** within the resources.

Then, fill in the necessary details in the turret configuration file for use.
```
---
🧩 Plugin Features

Supports multiple turret type configurations and multi-language support.

Supports menu selection for turret creation.

Supports custom turret base and physical models (if left blank, the models from the Workshop example above will be used).

Customizable menu access command (default: sw_turret). You can set your own command within the configuration, but it must start with sw_.

Allows for customization of various turret attributes with multiple personalization options:

Attribute,Description,Customization Details

Name,Unique Turret Name,

Enable,Turret switch (on/off),

Model,Turret Model Path,"e.g., models/turret.vmdl"

Range,Target acquisition radius,

Rate,Attack Frequency,0.1 = Attacks every 0.1 seconds

Damage,Custom damage per hit,

KnockBack,Custom push force upon hit,

FireAnim,Attack animation name,

Team,Team Restriction,all = Visible to all teams in the menu; ct = Only CTs can see and create

Price,Cost to purchase,"0 = Free; otherwise, requires money"

Limit,Creation Limit,"0 = Unlimited creation; otherwise, set a maximum per player"

Permissions,Admin Permissions,"Leave blank for no permission required; otherwise, requires a specific permission flag"

GlowColor,ESP/Glow Outline Color,"Leave blank to disable glow; otherwise, set using RGBA values (e.g., 255,0,0,255)"

laserColor,Laser Beam Effect Color,"Leave blank to disable laser; otherwise, set using RGBA values"

TurretFireSound,Turret Fire Sound,Fill in the soundevent name to play the attack sound

PrecacheSoundEvent,Precache Sound Event,Fill in the sound event file path to precache the sound events

MuzzleParticle,Muzzle Flash Particle,(Built-in precache)

MuzzleAttachment,Muzzle Attachment Point,"Requires finding the attachment point name from your turret model to attach the particle. 

Multiple attachments are separated by commas (,). The plugin will set particles according to the number of attachments."

Supports free expansion of multiple turret attributes.
---
🧱 Configuration Example (Excerpt)

You can freely set different turret attributes:
```
{
  "HanTurretS2CFG": {
    "TurretList": [
		{
			"Enable": true,
			"Name": "TurretCT(level1)",
			"Model": "models/stk_sentry_guns/sentry/lvl_1_ct.vmdl",
			"Range": 300,
			"Rate": 0.5,
			"Damage": 5,
			"KnockBack": 1000,
			"FireAnim": "fire",
			"Team": "ct",
			"Price": "0",
			"Limit": 0,
			"Permissions": "",
			"GlowColor": "0,0,255,255",
			"laserColor": "0,0,255,255",
			"TurretFireSound": "n4a_csdm_sentry.sentry_shoot",
			"PrecacheSoundEvent": "soundevents/n4a_csdm_sentry.vsndevts",
			"MuzzleParticle": "particles/stk_sentryguns/sparks_muzzle_core.vpcf",
			"MuzzleAttachment": "fire_pos_1"
		},
		{
			"Enable": true,
			"Name": "TurretCT(level2)",
			"Model": "models/stk_sentry_guns/sentry/lvl_2_ct.vmdl",
			"Range": 500,
			"Rate": 0.4,
			"Damage": 20,
			"KnockBack": 1005,
			"FireAnim": "fire",
			"Team": "ct",
			"Price": "500",
			"Limit": 1,
			"Permissions": "",
			"GlowColor": "0,125,255,255",
			"laserColor": "0,125,255,255",
			"TurretFireSound": "n4a_csdm_sentry.sentry_shoot",
			"PrecacheSoundEvent": "soundevents/n4a_csdm_sentry.vsndevts",
			"MuzzleParticle": "particles/stk_sentryguns/sparks_muzzle_core.vpcf",
			"MuzzleAttachment": "fire_pos_1,fire_pos_2"
		}
	]
  }
}
```
