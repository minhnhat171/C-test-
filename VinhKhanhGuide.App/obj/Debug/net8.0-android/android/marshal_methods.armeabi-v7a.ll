; ModuleID = 'marshal_methods.armeabi-v7a.ll'
source_filename = "marshal_methods.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [334 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [662 x i32] [
	i32 2616222, ; 0: System.Net.NetworkInformation.dll => 0x27eb9e => 68
	i32 10166715, ; 1: System.Net.NameResolution.dll => 0x9b21bb => 67
	i32 15721112, ; 2: System.Runtime.Intrinsics.dll => 0xefe298 => 108
	i32 32687329, ; 3: Xamarin.AndroidX.Lifecycle.Runtime => 0x1f2c4e1 => 253
	i32 34715100, ; 4: Xamarin.Google.Guava.ListenableFuture.dll => 0x211b5dc => 287
	i32 34839235, ; 5: System.IO.FileSystem.DriveInfo => 0x2139ac3 => 48
	i32 39485524, ; 6: System.Net.WebSockets.dll => 0x25a8054 => 80
	i32 42639949, ; 7: System.Threading.Thread => 0x28aa24d => 143
	i32 53857724, ; 8: ca/Microsoft.Maui.Controls.resources => 0x335cdbc => 296
	i32 66541672, ; 9: System.Diagnostics.StackTrace => 0x3f75868 => 30
	i32 68219467, ; 10: System.Security.Cryptography.Primitives => 0x410f24b => 124
	i32 72070932, ; 11: Microsoft.Maui.Graphics.dll => 0x44bb714 => 195
	i32 82292897, ; 12: System.Runtime.CompilerServices.VisualC.dll => 0x4e7b0a1 => 102
	i32 101534019, ; 13: Xamarin.AndroidX.SlidingPaneLayout => 0x60d4943 => 271
	i32 113429830, ; 14: zh-HK/Microsoft.Maui.Controls.resources => 0x6c2cd46 => 326
	i32 117431740, ; 15: System.Runtime.InteropServices => 0x6ffddbc => 107
	i32 120558881, ; 16: Xamarin.AndroidX.SlidingPaneLayout.dll => 0x72f9521 => 271
	i32 122350210, ; 17: System.Threading.Channels.dll => 0x74aea82 => 137
	i32 134690465, ; 18: Xamarin.Kotlin.StdLib.Jdk7.dll => 0x80736a1 => 291
	i32 142721839, ; 19: System.Net.WebHeaderCollection => 0x881c32f => 77
	i32 149764678, ; 20: Svg.Skia.dll => 0x8ed3a46 => 207
	i32 149972175, ; 21: System.Security.Cryptography.Primitives.dll => 0x8f064cf => 124
	i32 159306688, ; 22: System.ComponentModel.Annotations => 0x97ed3c0 => 13
	i32 165246403, ; 23: Xamarin.AndroidX.Collection.dll => 0x9d975c3 => 227
	i32 176265551, ; 24: System.ServiceProcess => 0xa81994f => 132
	i32 182336117, ; 25: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 273
	i32 184328833, ; 26: System.ValueTuple.dll => 0xafca281 => 149
	i32 195452805, ; 27: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 325
	i32 199333315, ; 28: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 326
	i32 205061960, ; 29: System.ComponentModel => 0xc38ff48 => 18
	i32 209399409, ; 30: Xamarin.AndroidX.Browser.dll => 0xc7b2e71 => 225
	i32 220171995, ; 31: System.Diagnostics.Debug => 0xd1f8edb => 26
	i32 230216969, ; 32: Xamarin.AndroidX.Legacy.Support.Core.Utils.dll => 0xdb8d509 => 247
	i32 230752869, ; 33: Microsoft.CSharp.dll => 0xdc10265 => 1
	i32 231409092, ; 34: System.Linq.Parallel => 0xdcb05c4 => 59
	i32 231814094, ; 35: System.Globalization => 0xdd133ce => 42
	i32 246610117, ; 36: System.Reflection.Emit.Lightweight => 0xeb2f8c5 => 91
	i32 261689757, ; 37: Xamarin.AndroidX.ConstraintLayout.dll => 0xf99119d => 230
	i32 276479776, ; 38: System.Threading.Timer.dll => 0x107abf20 => 145
	i32 278686392, ; 39: Xamarin.AndroidX.Lifecycle.LiveData.dll => 0x109c6ab8 => 249
	i32 280482487, ; 40: Xamarin.AndroidX.Interpolator => 0x10b7d2b7 => 246
	i32 280992041, ; 41: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 297
	i32 291076382, ; 42: System.IO.Pipes.AccessControl.dll => 0x1159791e => 54
	i32 298918909, ; 43: System.Net.Ping.dll => 0x11d123fd => 69
	i32 318968648, ; 44: Xamarin.AndroidX.Activity.dll => 0x13031348 => 216
	i32 321597661, ; 45: System.Numerics => 0x132b30dd => 83
	i32 336156722, ; 46: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 310
	i32 342366114, ; 47: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 248
	i32 356389973, ; 48: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 309
	i32 357576608, ; 49: cs/Microsoft.Maui.Controls.resources => 0x15502fa0 => 297
	i32 360082299, ; 50: System.ServiceModel.Web => 0x15766b7b => 131
	i32 367780167, ; 51: System.IO.Pipes => 0x15ebe147 => 55
	i32 374914964, ; 52: System.Transactions.Local => 0x1658bf94 => 147
	i32 375677976, ; 53: System.Net.ServicePoint.dll => 0x16646418 => 74
	i32 379916513, ; 54: System.Threading.Thread.dll => 0x16a510e1 => 143
	i32 385762202, ; 55: System.Memory.dll => 0x16fe439a => 62
	i32 392610295, ; 56: System.Threading.ThreadPool.dll => 0x1766c1f7 => 144
	i32 395744057, ; 57: _Microsoft.Android.Resource.Designer => 0x17969339 => 330
	i32 403441872, ; 58: WindowsBase => 0x180c08d0 => 163
	i32 435591531, ; 59: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 321
	i32 441335492, ; 60: Xamarin.AndroidX.ConstraintLayout.Core => 0x1a4e3ec4 => 231
	i32 442565967, ; 61: System.Collections => 0x1a61054f => 12
	i32 450948140, ; 62: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 244
	i32 451504562, ; 63: System.Security.Cryptography.X509Certificates => 0x1ae969b2 => 125
	i32 456227837, ; 64: System.Web.HttpUtility.dll => 0x1b317bfd => 150
	i32 459347974, ; 65: System.Runtime.Serialization.Primitives.dll => 0x1b611806 => 113
	i32 465658307, ; 66: ExCSS => 0x1bc161c3 => 173
	i32 465846621, ; 67: mscorlib => 0x1bc4415d => 164
	i32 469710990, ; 68: System.dll => 0x1bff388e => 162
	i32 469965489, ; 69: Svg.Model => 0x1c031ab1 => 206
	i32 476646585, ; 70: Xamarin.AndroidX.Interpolator.dll => 0x1c690cb9 => 246
	i32 486930444, ; 71: Xamarin.AndroidX.LocalBroadcastManager.dll => 0x1d05f80c => 259
	i32 498788369, ; 72: System.ObjectModel => 0x1dbae811 => 84
	i32 500358224, ; 73: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 308
	i32 503918385, ; 74: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 302
	i32 513247710, ; 75: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 189
	i32 525008092, ; 76: SkiaSharp.dll => 0x1f4afcdc => 200
	i32 526420162, ; 77: System.Transactions.dll => 0x1f6088c2 => 148
	i32 527168573, ; 78: hi/Microsoft.Maui.Controls.resources => 0x1f6bf43d => 305
	i32 527452488, ; 79: Xamarin.Kotlin.StdLib.Jdk7 => 0x1f704948 => 291
	i32 530272170, ; 80: System.Linq.Queryable => 0x1f9b4faa => 60
	i32 539058512, ; 81: Microsoft.Extensions.Logging => 0x20216150 => 185
	i32 540030774, ; 82: System.IO.FileSystem.dll => 0x20303736 => 51
	i32 545304856, ; 83: System.Runtime.Extensions => 0x2080b118 => 103
	i32 546455878, ; 84: System.Runtime.Serialization.Xml => 0x20924146 => 114
	i32 549171840, ; 85: System.Globalization.Calendars => 0x20bbb280 => 40
	i32 557405415, ; 86: Jsr305Binding => 0x213954e7 => 284
	i32 569601784, ; 87: Xamarin.AndroidX.Window.Extensions.Core.Core => 0x21f36ef8 => 282
	i32 577335427, ; 88: System.Security.Cryptography.Cng => 0x22697083 => 120
	i32 592146354, ; 89: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 316
	i32 601371474, ; 90: System.IO.IsolatedStorage.dll => 0x23d83352 => 52
	i32 605376203, ; 91: System.IO.Compression.FileSystem => 0x24154ecb => 44
	i32 613668793, ; 92: System.Security.Cryptography.Algorithms => 0x2493d7b9 => 119
	i32 627609679, ; 93: Xamarin.AndroidX.CustomView => 0x2568904f => 236
	i32 639843206, ; 94: Xamarin.AndroidX.Emoji2.ViewsHelper.dll => 0x26233b86 => 242
	i32 643868501, ; 95: System.Net => 0x2660a755 => 81
	i32 662205335, ; 96: System.Text.Encodings.Web.dll => 0x27787397 => 209
	i32 663517072, ; 97: Xamarin.AndroidX.VersionedParcelable => 0x278c7790 => 278
	i32 666292255, ; 98: Xamarin.AndroidX.Arch.Core.Common.dll => 0x27b6d01f => 223
	i32 672442732, ; 99: System.Collections.Concurrent => 0x2814a96c => 8
	i32 680049820, ; 100: Mapsui.Rendering.Skia.dll => 0x2888bc9c => 179
	i32 683518922, ; 101: System.Net.Security => 0x28bdabca => 73
	i32 688181140, ; 102: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 296
	i32 690569205, ; 103: System.Xml.Linq.dll => 0x29293ff5 => 153
	i32 691348768, ; 104: Xamarin.KotlinX.Coroutines.Android.dll => 0x29352520 => 293
	i32 693804605, ; 105: System.Windows => 0x295a9e3d => 152
	i32 699345723, ; 106: System.Reflection.Emit => 0x29af2b3b => 92
	i32 700284507, ; 107: Xamarin.Jetbrains.Annotations => 0x29bd7e5b => 288
	i32 700358131, ; 108: System.IO.Compression.ZipFile => 0x29be9df3 => 45
	i32 706645707, ; 109: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 311
	i32 709557578, ; 110: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 299
	i32 720511267, ; 111: Xamarin.Kotlin.StdLib.Jdk8 => 0x2af22123 => 292
	i32 722857257, ; 112: System.Runtime.Loader.dll => 0x2b15ed29 => 109
	i32 735137430, ; 113: System.Security.SecureString.dll => 0x2bd14e96 => 129
	i32 752232764, ; 114: System.Diagnostics.Contracts.dll => 0x2cd6293c => 25
	i32 755313932, ; 115: Xamarin.Android.Glide.Annotations.dll => 0x2d052d0c => 213
	i32 759454413, ; 116: System.Net.Requests => 0x2d445acd => 72
	i32 762598435, ; 117: System.IO.Pipes.dll => 0x2d745423 => 55
	i32 775507847, ; 118: System.IO.Compression => 0x2e394f87 => 46
	i32 778756650, ; 119: SkiaSharp.HarfBuzz.dll => 0x2e6ae22a => 201
	i32 789151979, ; 120: Microsoft.Extensions.Options => 0x2f0980eb => 188
	i32 790371945, ; 121: Xamarin.AndroidX.CustomView.PoolingContainer.dll => 0x2f1c1e69 => 237
	i32 804715423, ; 122: System.Data.Common => 0x2ff6fb9f => 22
	i32 807930345, ; 123: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll => 0x302809e9 => 251
	i32 823281589, ; 124: System.Private.Uri.dll => 0x311247b5 => 86
	i32 830298997, ; 125: System.IO.Compression.Brotli => 0x317d5b75 => 43
	i32 832635846, ; 126: System.Xml.XPath.dll => 0x31a103c6 => 158
	i32 834051424, ; 127: System.Net.Quic => 0x31b69d60 => 71
	i32 843511501, ; 128: Xamarin.AndroidX.Print => 0x3246f6cd => 264
	i32 870878177, ; 129: ar/Microsoft.Maui.Controls.resources => 0x33e88be1 => 295
	i32 873119928, ; 130: Microsoft.VisualBasic => 0x340ac0b8 => 3
	i32 877678880, ; 131: System.Globalization.dll => 0x34505120 => 42
	i32 878954865, ; 132: System.Net.Http.Json => 0x3463c971 => 63
	i32 899130691, ; 133: NetTopologySuite.dll => 0x3597a543 => 196
	i32 904024072, ; 134: System.ComponentModel.Primitives.dll => 0x35e25008 => 16
	i32 911108515, ; 135: System.IO.MemoryMappedFiles.dll => 0x364e69a3 => 53
	i32 926902833, ; 136: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 323
	i32 928116545, ; 137: Xamarin.Google.Guava.ListenableFuture => 0x3751ef41 => 287
	i32 952186615, ; 138: System.Runtime.InteropServices.JavaScript.dll => 0x38c136f7 => 105
	i32 956507658, ; 139: Mapsui.UI.Maui8.dll => 0x3903260a => 177
	i32 956575887, ; 140: Xamarin.Kotlin.StdLib.Jdk8.dll => 0x3904308f => 292
	i32 966729478, ; 141: Xamarin.Google.Crypto.Tink.Android => 0x399f1f06 => 285
	i32 967690846, ; 142: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 248
	i32 975236339, ; 143: System.Diagnostics.Tracing => 0x3a20ecf3 => 34
	i32 975874589, ; 144: System.Xml.XDocument => 0x3a2aaa1d => 156
	i32 986514023, ; 145: System.Private.DataContractSerialization.dll => 0x3acd0267 => 85
	i32 987214855, ; 146: System.Diagnostics.Tools => 0x3ad7b407 => 32
	i32 992768348, ; 147: System.Collections.dll => 0x3b2c715c => 12
	i32 993161700, ; 148: zh-Hans/Microsoft.Maui.Controls.resources => 0x3b3271e4 => 327
	i32 994442037, ; 149: System.IO.FileSystem => 0x3b45fb35 => 51
	i32 994547685, ; 150: es/Microsoft.Maui.Controls.resources => 0x3b4797e5 => 301
	i32 1001831731, ; 151: System.IO.UnmanagedMemoryStream.dll => 0x3bb6bd33 => 56
	i32 1012816738, ; 152: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 268
	i32 1019214401, ; 153: System.Drawing => 0x3cbffa41 => 36
	i32 1028951442, ; 154: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 184
	i32 1029334545, ; 155: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 298
	i32 1031528504, ; 156: Xamarin.Google.ErrorProne.Annotations.dll => 0x3d7be038 => 286
	i32 1035644815, ; 157: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 221
	i32 1036536393, ; 158: System.Drawing.Primitives.dll => 0x3dc84a49 => 35
	i32 1044663988, ; 159: System.Linq.Expressions.dll => 0x3e444eb4 => 58
	i32 1052210849, ; 160: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 255
	i32 1055389286, ; 161: Mapsui.UI.Maui8 => 0x3ee7f666 => 177
	i32 1067306892, ; 162: GoogleGson => 0x3f9dcf8c => 174
	i32 1082857460, ; 163: System.ComponentModel.TypeConverter => 0x408b17f4 => 17
	i32 1084122840, ; 164: Xamarin.Kotlin.StdLib => 0x409e66d8 => 289
	i32 1098259244, ; 165: System => 0x41761b2c => 162
	i32 1121599056, ; 166: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll => 0x42da3e50 => 254
	i32 1127624469, ; 167: Microsoft.Extensions.Logging.Debug => 0x43362f15 => 187
	i32 1149092582, ; 168: Xamarin.AndroidX.Window => 0x447dc2e6 => 281
	i32 1170634674, ; 169: System.Web.dll => 0x45c677b2 => 151
	i32 1175144683, ; 170: Xamarin.AndroidX.VectorDrawable.Animated => 0x460b48eb => 277
	i32 1178241025, ; 171: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 262
	i32 1178602117, ; 172: VinhKhanhGuide.App => 0x46400a85 => 0
	i32 1178797549, ; 173: fi/Microsoft.Maui.Controls.resources => 0x464305ed => 302
	i32 1203215381, ; 174: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 315
	i32 1204270330, ; 175: Xamarin.AndroidX.Arch.Core.Common => 0x47c7b4fa => 223
	i32 1208641965, ; 176: System.Diagnostics.Process => 0x480a69ad => 29
	i32 1219128291, ; 177: System.IO.IsolatedStorage => 0x48aa6be3 => 52
	i32 1234928153, ; 178: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 313
	i32 1240993432, ; 179: BruTile.XmlSerializers => 0x49f80e98 => 171
	i32 1243150071, ; 180: Xamarin.AndroidX.Window.Extensions.Core.Core.dll => 0x4a18f6f7 => 282
	i32 1253011324, ; 181: Microsoft.Win32.Registry => 0x4aaf6f7c => 5
	i32 1264511973, ; 182: Xamarin.AndroidX.Startup.StartupRuntime.dll => 0x4b5eebe5 => 272
	i32 1267360935, ; 183: Xamarin.AndroidX.VectorDrawable => 0x4b8a64a7 => 276
	i32 1273260888, ; 184: Xamarin.AndroidX.Collection.Ktx => 0x4be46b58 => 228
	i32 1275534314, ; 185: Xamarin.KotlinX.Coroutines.Android => 0x4c071bea => 293
	i32 1278448581, ; 186: Xamarin.AndroidX.Annotation.Jvm => 0x4c3393c5 => 220
	i32 1293217323, ; 187: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 239
	i32 1309188875, ; 188: System.Private.DataContractSerialization => 0x4e08a30b => 85
	i32 1313028017, ; 189: Topten.RichTextKit => 0x4e4337b1 => 211
	i32 1322716291, ; 190: Xamarin.AndroidX.Window.dll => 0x4ed70c83 => 281
	i32 1324164729, ; 191: System.Linq => 0x4eed2679 => 61
	i32 1335329327, ; 192: System.Runtime.Serialization.Json.dll => 0x4f97822f => 112
	i32 1364015309, ; 193: System.IO => 0x514d38cd => 57
	i32 1376866003, ; 194: Xamarin.AndroidX.SavedState => 0x52114ed3 => 268
	i32 1379779777, ; 195: System.Resources.ResourceManager => 0x523dc4c1 => 99
	i32 1388087747, ; 196: Mapsui.dll => 0x52bc89c3 => 176
	i32 1394563993, ; 197: VinhKhanhGuide.App.dll => 0x531f5b99 => 0
	i32 1402170036, ; 198: System.Configuration.dll => 0x53936ab4 => 19
	i32 1406073936, ; 199: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 232
	i32 1408764838, ; 200: System.Runtime.Serialization.Formatters.dll => 0x53f80ba6 => 111
	i32 1411638395, ; 201: System.Runtime.CompilerServices.Unsafe => 0x5423e47b => 101
	i32 1422545099, ; 202: System.Runtime.CompilerServices.VisualC => 0x54ca50cb => 102
	i32 1422967952, ; 203: Mapsui.Tiling.dll => 0x54d0c490 => 180
	i32 1434145427, ; 204: System.Runtime.Handles => 0x557b5293 => 104
	i32 1435222561, ; 205: Xamarin.Google.Crypto.Tink.Android.dll => 0x558bc221 => 285
	i32 1439761251, ; 206: System.Net.Quic.dll => 0x55d10363 => 71
	i32 1443938015, ; 207: NetTopologySuite => 0x5610bedf => 196
	i32 1452070440, ; 208: System.Formats.Asn1.dll => 0x568cd628 => 38
	i32 1453312822, ; 209: System.Diagnostics.Tools.dll => 0x569fcb36 => 32
	i32 1457743152, ; 210: System.Runtime.Extensions.dll => 0x56e36530 => 103
	i32 1458022317, ; 211: System.Net.Security.dll => 0x56e7a7ad => 73
	i32 1461234159, ; 212: System.Collections.Immutable.dll => 0x5718a9ef => 9
	i32 1461719063, ; 213: System.Security.Cryptography.OpenSsl => 0x57201017 => 123
	i32 1462112819, ; 214: System.IO.Compression.dll => 0x57261233 => 46
	i32 1469204771, ; 215: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 222
	i32 1470490898, ; 216: Microsoft.Extensions.Primitives => 0x57a5e912 => 189
	i32 1479771757, ; 217: System.Collections.Immutable => 0x5833866d => 9
	i32 1480492111, ; 218: System.IO.Compression.Brotli.dll => 0x583e844f => 43
	i32 1487239319, ; 219: Microsoft.Win32.Primitives => 0x58a57897 => 4
	i32 1490025113, ; 220: Xamarin.AndroidX.SavedState.SavedState.Ktx.dll => 0x58cffa99 => 269
	i32 1493001747, ; 221: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 305
	i32 1514721132, ; 222: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 300
	i32 1536373174, ; 223: System.Diagnostics.TextWriterTraceListener => 0x5b9331b6 => 31
	i32 1543031311, ; 224: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 136
	i32 1543355203, ; 225: System.Reflection.Emit.dll => 0x5bfdbb43 => 92
	i32 1550322496, ; 226: System.Reflection.Extensions.dll => 0x5c680b40 => 93
	i32 1551623176, ; 227: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 320
	i32 1554762148, ; 228: fr/Microsoft.Maui.Controls.resources => 0x5cabc9a4 => 303
	i32 1565862583, ; 229: System.IO.FileSystem.Primitives => 0x5d552ab7 => 49
	i32 1566207040, ; 230: System.Threading.Tasks.Dataflow.dll => 0x5d5a6c40 => 139
	i32 1573704789, ; 231: System.Runtime.Serialization.Json => 0x5dccd455 => 112
	i32 1580037396, ; 232: System.Threading.Overlapped => 0x5e2d7514 => 138
	i32 1580413037, ; 233: sv/Microsoft.Maui.Controls.resources => 0x5e33306d => 321
	i32 1582372066, ; 234: Xamarin.AndroidX.DocumentFile.dll => 0x5e5114e2 => 238
	i32 1591080825, ; 235: zh-Hant/Microsoft.Maui.Controls.resources => 0x5ed5f779 => 328
	i32 1592978981, ; 236: System.Runtime.Serialization.dll => 0x5ef2ee25 => 115
	i32 1597949149, ; 237: Xamarin.Google.ErrorProne.Annotations => 0x5f3ec4dd => 286
	i32 1600541741, ; 238: ShimSkiaSharp => 0x5f66542d => 199
	i32 1601112923, ; 239: System.Xml.Serialization => 0x5f6f0b5b => 155
	i32 1604827217, ; 240: System.Net.WebClient => 0x5fa7b851 => 76
	i32 1618516317, ; 241: System.Net.WebSockets.Client.dll => 0x6078995d => 79
	i32 1622152042, ; 242: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 258
	i32 1622358360, ; 243: System.Dynamic.Runtime => 0x60b33958 => 37
	i32 1623212457, ; 244: SkiaSharp.Views.Maui.Controls => 0x60c041a9 => 203
	i32 1624863272, ; 245: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 280
	i32 1635184631, ; 246: Xamarin.AndroidX.Emoji2.ViewsHelper => 0x6176eff7 => 242
	i32 1636350590, ; 247: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 235
	i32 1639515021, ; 248: System.Net.Http.dll => 0x61b9038d => 64
	i32 1639986890, ; 249: System.Text.RegularExpressions => 0x61c036ca => 136
	i32 1641389582, ; 250: System.ComponentModel.EventBasedAsync.dll => 0x61d59e0e => 15
	i32 1657153582, ; 251: System.Runtime => 0x62c6282e => 116
	i32 1658241508, ; 252: Xamarin.AndroidX.Tracing.Tracing.dll => 0x62d6c1e4 => 274
	i32 1658251792, ; 253: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 283
	i32 1670060433, ; 254: Xamarin.AndroidX.ConstraintLayout => 0x638b1991 => 230
	i32 1672364457, ; 255: NetTopologySuite.IO.GeoJSON4STJ.dll => 0x63ae41a9 => 198
	i32 1675553242, ; 256: System.IO.FileSystem.DriveInfo.dll => 0x63dee9da => 48
	i32 1677501392, ; 257: System.Net.Primitives.dll => 0x63fca3d0 => 70
	i32 1678508291, ; 258: System.Net.WebSockets => 0x640c0103 => 80
	i32 1679769178, ; 259: System.Security.Cryptography => 0x641f3e5a => 126
	i32 1691477237, ; 260: System.Reflection.Metadata => 0x64d1e4f5 => 94
	i32 1696967625, ; 261: System.Security.Cryptography.Csp => 0x6525abc9 => 121
	i32 1698840827, ; 262: Xamarin.Kotlin.StdLib.Common => 0x654240fb => 290
	i32 1701541528, ; 263: System.Diagnostics.Debug.dll => 0x656b7698 => 26
	i32 1720223769, ; 264: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx => 0x66888819 => 251
	i32 1726116996, ; 265: System.Reflection.dll => 0x66e27484 => 97
	i32 1728033016, ; 266: System.Diagnostics.FileVersionInfo.dll => 0x66ffb0f8 => 28
	i32 1729485958, ; 267: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 226
	i32 1736233607, ; 268: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 318
	i32 1744735666, ; 269: System.Transactions.Local.dll => 0x67fe8db2 => 147
	i32 1746115085, ; 270: System.IO.Pipelines.dll => 0x68139a0d => 208
	i32 1746316138, ; 271: Mono.Android.Export => 0x6816ab6a => 167
	i32 1750313021, ; 272: Microsoft.Win32.Primitives.dll => 0x6853a83d => 4
	i32 1758240030, ; 273: System.Resources.Reader.dll => 0x68cc9d1e => 98
	i32 1763938596, ; 274: System.Diagnostics.TraceSource.dll => 0x69239124 => 33
	i32 1765942094, ; 275: System.Reflection.Extensions => 0x6942234e => 93
	i32 1766324549, ; 276: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 273
	i32 1770582343, ; 277: Microsoft.Extensions.Logging.dll => 0x6988f147 => 185
	i32 1776026572, ; 278: System.Core.dll => 0x69dc03cc => 21
	i32 1777075843, ; 279: System.Globalization.Extensions.dll => 0x69ec0683 => 41
	i32 1780572499, ; 280: Mono.Android.Runtime.dll => 0x6a216153 => 168
	i32 1788241197, ; 281: Xamarin.AndroidX.Fragment => 0x6a96652d => 244
	i32 1808609942, ; 282: Xamarin.AndroidX.Loader => 0x6bcd3296 => 258
	i32 1809966115, ; 283: nb/Microsoft.Maui.Controls.resources => 0x6be1e423 => 313
	i32 1813058853, ; 284: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 289
	i32 1813201214, ; 285: Xamarin.Google.Android.Material => 0x6c13413e => 283
	i32 1818569960, ; 286: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 263
	i32 1818787751, ; 287: Microsoft.VisualBasic.Core => 0x6c687fa7 => 2
	i32 1821794637, ; 288: hu/Microsoft.Maui.Controls.resources => 0x6c96614d => 307
	i32 1824175904, ; 289: System.Text.Encoding.Extensions => 0x6cbab720 => 134
	i32 1824722060, ; 290: System.Runtime.Serialization.Formatters => 0x6cc30c8c => 111
	i32 1828688058, ; 291: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 186
	i32 1839733746, ; 292: Mapsui.Nts.dll => 0x6da81bf2 => 178
	i32 1842015223, ; 293: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 324
	i32 1847515442, ; 294: Xamarin.Android.Glide.Annotations => 0x6e1ed932 => 213
	i32 1858542181, ; 295: System.Linq.Expressions => 0x6ec71a65 => 58
	i32 1859970510, ; 296: VinhKhanhGuide.Core.dll => 0x6edce5ce => 329
	i32 1870277092, ; 297: System.Reflection.Primitives => 0x6f7a29e4 => 95
	i32 1879696579, ; 298: System.Formats.Tar.dll => 0x7009e4c3 => 39
	i32 1885316902, ; 299: Xamarin.AndroidX.Arch.Core.Runtime.dll => 0x705fa726 => 224
	i32 1888955245, ; 300: System.Diagnostics.Contracts => 0x70972b6d => 25
	i32 1889954781, ; 301: System.Reflection.Metadata.dll => 0x70a66bdd => 94
	i32 1898237753, ; 302: System.Reflection.DispatchProxy => 0x7124cf39 => 89
	i32 1900610850, ; 303: System.Resources.ResourceManager.dll => 0x71490522 => 99
	i32 1910275211, ; 304: System.Collections.NonGeneric.dll => 0x71dc7c8b => 10
	i32 1939592360, ; 305: System.Private.Xml.Linq => 0x739bd4a8 => 87
	i32 1956758971, ; 306: System.Resources.Writer => 0x74a1c5bb => 100
	i32 1960264639, ; 307: ja/Microsoft.Maui.Controls.resources => 0x74d743bf => 310
	i32 1961813231, ; 308: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x74eee4ef => 270
	i32 1968388702, ; 309: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 181
	i32 1983156543, ; 310: Xamarin.Kotlin.StdLib.Common.dll => 0x7634913f => 290
	i32 1985761444, ; 311: Xamarin.Android.Glide.GifDecoder => 0x765c50a4 => 215
	i32 2011961780, ; 312: System.Buffers.dll => 0x77ec19b4 => 7
	i32 2014344398, ; 313: hr/Microsoft.Maui.Controls.resources => 0x781074ce => 306
	i32 2019465201, ; 314: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 255
	i32 2025202353, ; 315: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 295
	i32 2031763787, ; 316: Xamarin.Android.Glide => 0x791a414b => 212
	i32 2043674646, ; 317: it/Microsoft.Maui.Controls.resources => 0x79d00016 => 309
	i32 2045470958, ; 318: System.Private.Xml => 0x79eb68ee => 88
	i32 2055257422, ; 319: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 250
	i32 2060060697, ; 320: System.Windows.dll => 0x7aca0819 => 152
	i32 2070888862, ; 321: System.Diagnostics.TraceSource => 0x7b6f419e => 33
	i32 2079903147, ; 322: System.Runtime.dll => 0x7bf8cdab => 116
	i32 2090596640, ; 323: System.Numerics.Vectors => 0x7c9bf920 => 82
	i32 2127167465, ; 324: System.Console => 0x7ec9ffe9 => 20
	i32 2142473426, ; 325: System.Collections.Specialized => 0x7fb38cd2 => 11
	i32 2143790110, ; 326: System.Xml.XmlSerializer.dll => 0x7fc7a41e => 160
	i32 2146852085, ; 327: Microsoft.VisualBasic.dll => 0x7ff65cf5 => 3
	i32 2150663486, ; 328: ko/Microsoft.Maui.Controls.resources => 0x8030853e => 311
	i32 2159891885, ; 329: Microsoft.Maui => 0x80bd55ad => 193
	i32 2165051842, ; 330: ro/Microsoft.Maui.Controls.resources => 0x810c11c2 => 318
	i32 2181898931, ; 331: Microsoft.Extensions.Options.dll => 0x820d22b3 => 188
	i32 2192057212, ; 332: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 186
	i32 2193016926, ; 333: System.ObjectModel.dll => 0x82b6c85e => 84
	i32 2201107256, ; 334: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 294
	i32 2201231467, ; 335: System.Net.Http => 0x8334206b => 64
	i32 2217644978, ; 336: Xamarin.AndroidX.VectorDrawable.Animated.dll => 0x842e93b2 => 277
	i32 2222056684, ; 337: System.Threading.Tasks.Parallel => 0x8471e4ec => 141
	i32 2244775296, ; 338: Xamarin.AndroidX.LocalBroadcastManager => 0x85cc8d80 => 259
	i32 2252106437, ; 339: System.Xml.Serialization.dll => 0x863c6ac5 => 155
	i32 2256313426, ; 340: System.Globalization.Extensions => 0x867c9c52 => 41
	i32 2265110946, ; 341: System.Security.AccessControl.dll => 0x8702d9a2 => 117
	i32 2266799131, ; 342: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 182
	i32 2267999099, ; 343: Xamarin.Android.Glide.DiskLruCache.dll => 0x872eeb7b => 214
	i32 2270573516, ; 344: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 303
	i32 2279755925, ; 345: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 266
	i32 2289298199, ; 346: th/Microsoft.Maui.Controls.resources => 0x8873eb17 => 322
	i32 2293034957, ; 347: System.ServiceModel.Web.dll => 0x88acefcd => 131
	i32 2295906218, ; 348: System.Net.Sockets => 0x88d8bfaa => 75
	i32 2298471582, ; 349: System.Net.Mail => 0x88ffe49e => 66
	i32 2305521784, ; 350: System.Private.CoreLib.dll => 0x896b7878 => 170
	i32 2315684594, ; 351: Xamarin.AndroidX.Annotation.dll => 0x8a068af2 => 218
	i32 2320631194, ; 352: System.Threading.Tasks.Parallel.dll => 0x8a52059a => 141
	i32 2327893114, ; 353: ExCSS.dll => 0x8ac0d47a => 173
	i32 2340441535, ; 354: System.Runtime.InteropServices.RuntimeInformation.dll => 0x8b804dbf => 106
	i32 2344264397, ; 355: System.ValueTuple => 0x8bbaa2cd => 149
	i32 2353062107, ; 356: System.Net.Primitives => 0x8c40e0db => 70
	i32 2364201794, ; 357: SkiaSharp.Views.Maui.Core => 0x8ceadb42 => 204
	i32 2368005991, ; 358: System.Xml.ReaderWriter.dll => 0x8d24e767 => 154
	i32 2369760409, ; 359: tr/Microsoft.Maui.Controls.resources => 0x8d3fac99 => 323
	i32 2371007202, ; 360: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 181
	i32 2378619854, ; 361: System.Security.Cryptography.Csp.dll => 0x8dc6dbce => 121
	i32 2383496789, ; 362: System.Security.Principal.Windows.dll => 0x8e114655 => 127
	i32 2401565422, ; 363: System.Web.HttpUtility => 0x8f24faee => 150
	i32 2403452196, ; 364: Xamarin.AndroidX.Emoji2.dll => 0x8f41c524 => 241
	i32 2421380589, ; 365: System.Threading.Tasks.Dataflow => 0x905355ed => 139
	i32 2421992093, ; 366: nl/Microsoft.Maui.Controls.resources => 0x905caa9d => 314
	i32 2423080555, ; 367: Xamarin.AndroidX.Collection.Ktx.dll => 0x906d466b => 228
	i32 2435356389, ; 368: System.Console.dll => 0x912896e5 => 20
	i32 2435904999, ; 369: System.ComponentModel.DataAnnotations.dll => 0x9130f5e7 => 14
	i32 2454642406, ; 370: System.Text.Encoding.dll => 0x924edee6 => 135
	i32 2458678730, ; 371: System.Net.Sockets.dll => 0x928c75ca => 75
	i32 2459001652, ; 372: System.Linq.Parallel.dll => 0x92916334 => 59
	i32 2465532216, ; 373: Xamarin.AndroidX.ConstraintLayout.Core.dll => 0x92f50938 => 231
	i32 2471841756, ; 374: netstandard.dll => 0x93554fdc => 165
	i32 2475788418, ; 375: Java.Interop.dll => 0x93918882 => 166
	i32 2480646305, ; 376: Microsoft.Maui.Controls => 0x93dba8a1 => 191
	i32 2483903535, ; 377: System.ComponentModel.EventBasedAsync => 0x940d5c2f => 15
	i32 2484371297, ; 378: System.Net.ServicePoint => 0x94147f61 => 74
	i32 2490993605, ; 379: System.AppContext.dll => 0x94798bc5 => 6
	i32 2501346920, ; 380: System.Data.DataSetExtensions => 0x95178668 => 23
	i32 2505896520, ; 381: Xamarin.AndroidX.Lifecycle.Runtime.dll => 0x955cf248 => 253
	i32 2520433370, ; 382: sk/Microsoft.Maui.Controls.resources => 0x963ac2da => 320
	i32 2522472828, ; 383: Xamarin.Android.Glide.dll => 0x9659e17c => 212
	i32 2523023297, ; 384: Svg.Custom.dll => 0x966247c1 => 205
	i32 2538310050, ; 385: System.Reflection.Emit.Lightweight.dll => 0x974b89a2 => 91
	i32 2562349572, ; 386: Microsoft.CSharp => 0x98ba5a04 => 1
	i32 2570120770, ; 387: System.Text.Encodings.Web => 0x9930ee42 => 209
	i32 2577414832, ; 388: Mapsui.Nts => 0x99a03ab0 => 178
	i32 2581783588, ; 389: Xamarin.AndroidX.Lifecycle.Runtime.Ktx => 0x99e2e424 => 254
	i32 2581819634, ; 390: Xamarin.AndroidX.VectorDrawable.dll => 0x99e370f2 => 276
	i32 2585220780, ; 391: System.Text.Encoding.Extensions.dll => 0x9a1756ac => 134
	i32 2585805581, ; 392: System.Net.Ping => 0x9a20430d => 69
	i32 2589602615, ; 393: System.Threading.ThreadPool => 0x9a5a3337 => 144
	i32 2602257211, ; 394: Svg.Model.dll => 0x9b1b4b3b => 206
	i32 2605712449, ; 395: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 294
	i32 2609324236, ; 396: Svg.Custom => 0x9b8720cc => 205
	i32 2615233544, ; 397: Xamarin.AndroidX.Fragment.Ktx => 0x9be14c08 => 245
	i32 2616218305, ; 398: Microsoft.Extensions.Logging.Debug.dll => 0x9bf052c1 => 187
	i32 2617129537, ; 399: System.Private.Xml.dll => 0x9bfe3a41 => 88
	i32 2618712057, ; 400: System.Reflection.TypeExtensions.dll => 0x9c165ff9 => 96
	i32 2620871830, ; 401: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 235
	i32 2624644809, ; 402: Xamarin.AndroidX.DynamicAnimation => 0x9c70e6c9 => 240
	i32 2625339995, ; 403: SkiaSharp.Views.Maui.Core.dll => 0x9c7b825b => 204
	i32 2627185994, ; 404: System.Diagnostics.TextWriterTraceListener.dll => 0x9c97ad4a => 31
	i32 2629843544, ; 405: System.IO.Compression.ZipFile.dll => 0x9cc03a58 => 45
	i32 2633051222, ; 406: Xamarin.AndroidX.Lifecycle.LiveData => 0x9cf12c56 => 249
	i32 2663391936, ; 407: Xamarin.Android.Glide.DiskLruCache => 0x9ec022c0 => 214
	i32 2663698177, ; 408: System.Runtime.Loader => 0x9ec4cf01 => 109
	i32 2664396074, ; 409: System.Xml.XDocument.dll => 0x9ecf752a => 156
	i32 2665622720, ; 410: System.Drawing.Primitives => 0x9ee22cc0 => 35
	i32 2676780864, ; 411: System.Data.Common.dll => 0x9f8c6f40 => 22
	i32 2686887180, ; 412: System.Runtime.Serialization.Xml.dll => 0xa026a50c => 114
	i32 2693849962, ; 413: System.IO.dll => 0xa090e36a => 57
	i32 2701096212, ; 414: Xamarin.AndroidX.Tracing.Tracing => 0xa0ff7514 => 274
	i32 2715334215, ; 415: System.Threading.Tasks.dll => 0xa1d8b647 => 142
	i32 2717744543, ; 416: System.Security.Claims => 0xa1fd7d9f => 118
	i32 2719963679, ; 417: System.Security.Cryptography.Cng.dll => 0xa21f5a1f => 120
	i32 2724373263, ; 418: System.Runtime.Numerics.dll => 0xa262a30f => 110
	i32 2732626843, ; 419: Xamarin.AndroidX.Activity => 0xa2e0939b => 216
	i32 2735172069, ; 420: System.Threading.Channels => 0xa30769e5 => 137
	i32 2737747696, ; 421: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 222
	i32 2740948882, ; 422: System.IO.Pipes.AccessControl => 0xa35f8f92 => 54
	i32 2748088231, ; 423: System.Runtime.InteropServices.JavaScript => 0xa3cc7fa7 => 105
	i32 2756874198, ; 424: NetTopologySuite.IO.GeoJSON4STJ => 0xa4528fd6 => 198
	i32 2758225723, ; 425: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 192
	i32 2764765095, ; 426: Microsoft.Maui.dll => 0xa4caf7a7 => 193
	i32 2765824710, ; 427: System.Text.Encoding.CodePages.dll => 0xa4db22c6 => 133
	i32 2770495804, ; 428: Xamarin.Jetbrains.Annotations.dll => 0xa522693c => 288
	i32 2778768386, ; 429: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 279
	i32 2779977773, ; 430: Xamarin.AndroidX.ResourceInspection.Annotation.dll => 0xa5b3182d => 267
	i32 2788224221, ; 431: Xamarin.AndroidX.Fragment.Ktx.dll => 0xa630ecdd => 245
	i32 2795602088, ; 432: SkiaSharp.Views.Android.dll => 0xa6a180a8 => 202
	i32 2801831435, ; 433: Microsoft.Maui.Graphics => 0xa7008e0b => 195
	i32 2802068195, ; 434: uk/Microsoft.Maui.Controls.resources => 0xa7042ae3 => 324
	i32 2803228030, ; 435: System.Xml.XPath.XDocument.dll => 0xa715dd7e => 157
	i32 2806116107, ; 436: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 301
	i32 2810250172, ; 437: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 232
	i32 2819470561, ; 438: System.Xml.dll => 0xa80db4e1 => 161
	i32 2821205001, ; 439: System.ServiceProcess.dll => 0xa8282c09 => 132
	i32 2821294376, ; 440: Xamarin.AndroidX.ResourceInspection.Annotation => 0xa8298928 => 267
	i32 2824502124, ; 441: System.Xml.XmlDocument => 0xa85a7b6c => 159
	i32 2831556043, ; 442: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 314
	i32 2838993487, ; 443: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll => 0xa9379a4f => 256
	i32 2849599387, ; 444: System.Threading.Overlapped.dll => 0xa9d96f9b => 138
	i32 2853208004, ; 445: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 279
	i32 2855708567, ; 446: Xamarin.AndroidX.Transition => 0xaa36a797 => 275
	i32 2857259519, ; 447: el/Microsoft.Maui.Controls.resources => 0xaa4e51ff => 300
	i32 2861098320, ; 448: Mono.Android.Export.dll => 0xaa88e550 => 167
	i32 2861189240, ; 449: Microsoft.Maui.Essentials => 0xaa8a4878 => 194
	i32 2870099610, ; 450: Xamarin.AndroidX.Activity.Ktx.dll => 0xab123e9a => 217
	i32 2875164099, ; 451: Jsr305Binding.dll => 0xab5f85c3 => 284
	i32 2875220617, ; 452: System.Globalization.Calendars.dll => 0xab606289 => 40
	i32 2883495834, ; 453: ru/Microsoft.Maui.Controls.resources => 0xabdea79a => 319
	i32 2884993177, ; 454: Xamarin.AndroidX.ExifInterface => 0xabf58099 => 243
	i32 2887636118, ; 455: System.Net.dll => 0xac1dd496 => 81
	i32 2899753641, ; 456: System.IO.UnmanagedMemoryStream => 0xacd6baa9 => 56
	i32 2900621748, ; 457: System.Dynamic.Runtime.dll => 0xace3f9b4 => 37
	i32 2901442782, ; 458: System.Reflection => 0xacf080de => 97
	i32 2905242038, ; 459: mscorlib.dll => 0xad2a79b6 => 164
	i32 2909740682, ; 460: System.Private.CoreLib => 0xad6f1e8a => 170
	i32 2912489636, ; 461: SkiaSharp.Views.Android => 0xad9910a4 => 202
	i32 2916838712, ; 462: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 280
	i32 2919462931, ; 463: System.Numerics.Vectors.dll => 0xae037813 => 82
	i32 2921128767, ; 464: Xamarin.AndroidX.Annotation.Experimental.dll => 0xae1ce33f => 219
	i32 2936416060, ; 465: System.Resources.Reader => 0xaf06273c => 98
	i32 2940926066, ; 466: System.Diagnostics.StackTrace.dll => 0xaf4af872 => 30
	i32 2942453041, ; 467: System.Xml.XPath.XDocument => 0xaf624531 => 157
	i32 2959614098, ; 468: System.ComponentModel.dll => 0xb0682092 => 18
	i32 2968338931, ; 469: System.Security.Principal.Windows => 0xb0ed41f3 => 127
	i32 2972252294, ; 470: System.Security.Cryptography.Algorithms.dll => 0xb128f886 => 119
	i32 2978675010, ; 471: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 239
	i32 2987532451, ; 472: Xamarin.AndroidX.Security.SecurityCrypto => 0xb21220a3 => 270
	i32 2996846495, ; 473: Xamarin.AndroidX.Lifecycle.Process.dll => 0xb2a03f9f => 252
	i32 3016983068, ; 474: Xamarin.AndroidX.Startup.StartupRuntime => 0xb3d3821c => 272
	i32 3023353419, ; 475: WindowsBase.dll => 0xb434b64b => 163
	i32 3024354802, ; 476: Xamarin.AndroidX.Legacy.Support.Core.Utils => 0xb443fdf2 => 247
	i32 3038032645, ; 477: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 330
	i32 3056245963, ; 478: Xamarin.AndroidX.SavedState.SavedState.Ktx => 0xb62a9ccb => 269
	i32 3057625584, ; 479: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 260
	i32 3059408633, ; 480: Mono.Android.Runtime => 0xb65adef9 => 168
	i32 3059793426, ; 481: System.ComponentModel.Primitives => 0xb660be12 => 16
	i32 3075834255, ; 482: System.Threading.Tasks => 0xb755818f => 142
	i32 3077302341, ; 483: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 307
	i32 3090735792, ; 484: System.Security.Cryptography.X509Certificates.dll => 0xb838e2b0 => 125
	i32 3099732863, ; 485: System.Security.Claims.dll => 0xb8c22b7f => 118
	i32 3103600923, ; 486: System.Formats.Asn1 => 0xb8fd311b => 38
	i32 3111772706, ; 487: System.Runtime.Serialization => 0xb979e222 => 115
	i32 3121463068, ; 488: System.IO.FileSystem.AccessControl.dll => 0xba0dbf1c => 47
	i32 3124832203, ; 489: System.Threading.Tasks.Extensions => 0xba4127cb => 140
	i32 3132293585, ; 490: System.Security.AccessControl => 0xbab301d1 => 117
	i32 3134694676, ; 491: ShimSkiaSharp.dll => 0xbad7a514 => 199
	i32 3147165239, ; 492: System.Diagnostics.Tracing.dll => 0xbb95ee37 => 34
	i32 3148237826, ; 493: GoogleGson.dll => 0xbba64c02 => 174
	i32 3159123045, ; 494: System.Reflection.Primitives.dll => 0xbc4c6465 => 95
	i32 3160747431, ; 495: System.IO.MemoryMappedFiles => 0xbc652da7 => 53
	i32 3178803400, ; 496: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 261
	i32 3192346100, ; 497: System.Security.SecureString => 0xbe4755f4 => 129
	i32 3193515020, ; 498: System.Web => 0xbe592c0c => 151
	i32 3204380047, ; 499: System.Data.dll => 0xbefef58f => 24
	i32 3209718065, ; 500: System.Xml.XmlDocument.dll => 0xbf506931 => 159
	i32 3211777861, ; 501: Xamarin.AndroidX.DocumentFile => 0xbf6fd745 => 238
	i32 3220365878, ; 502: System.Threading => 0xbff2e236 => 146
	i32 3226221578, ; 503: System.Runtime.Handles.dll => 0xc04c3c0a => 104
	i32 3251039220, ; 504: System.Reflection.DispatchProxy.dll => 0xc1c6ebf4 => 89
	i32 3258312781, ; 505: Xamarin.AndroidX.CardView => 0xc235e84d => 226
	i32 3265493905, ; 506: System.Linq.Queryable.dll => 0xc2a37b91 => 60
	i32 3265893370, ; 507: System.Threading.Tasks.Extensions.dll => 0xc2a993fa => 140
	i32 3277815716, ; 508: System.Resources.Writer.dll => 0xc35f7fa4 => 100
	i32 3278552754, ; 509: Mapsui => 0xc36abeb2 => 176
	i32 3279906254, ; 510: Microsoft.Win32.Registry.dll => 0xc37f65ce => 5
	i32 3280506390, ; 511: System.ComponentModel.Annotations.dll => 0xc3888e16 => 13
	i32 3290767353, ; 512: System.Security.Cryptography.Encoding => 0xc4251ff9 => 122
	i32 3299363146, ; 513: System.Text.Encoding => 0xc4a8494a => 135
	i32 3303498502, ; 514: System.Diagnostics.FileVersionInfo => 0xc4e76306 => 28
	i32 3316684772, ; 515: System.Net.Requests.dll => 0xc5b097e4 => 72
	i32 3317135071, ; 516: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 236
	i32 3317144872, ; 517: System.Data => 0xc5b79d28 => 24
	i32 3340387945, ; 518: SkiaSharp => 0xc71a4669 => 200
	i32 3340431453, ; 519: Xamarin.AndroidX.Arch.Core.Runtime => 0xc71af05d => 224
	i32 3345895724, ; 520: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll => 0xc76e512c => 265
	i32 3346324047, ; 521: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 262
	i32 3358260929, ; 522: System.Text.Json => 0xc82afec1 => 210
	i32 3362336904, ; 523: Xamarin.AndroidX.Activity.Ktx => 0xc8693088 => 217
	i32 3362522851, ; 524: Xamarin.AndroidX.Core => 0xc86c06e3 => 233
	i32 3366347497, ; 525: Java.Interop => 0xc8a662e9 => 166
	i32 3374999561, ; 526: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 266
	i32 3395150330, ; 527: System.Runtime.CompilerServices.Unsafe.dll => 0xca5de1fa => 101
	i32 3403906625, ; 528: System.Security.Cryptography.OpenSsl.dll => 0xcae37e41 => 123
	i32 3405233483, ; 529: Xamarin.AndroidX.CustomView.PoolingContainer => 0xcaf7bd4b => 237
	i32 3428513518, ; 530: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 183
	i32 3429136800, ; 531: System.Xml => 0xcc6479a0 => 161
	i32 3430777524, ; 532: netstandard => 0xcc7d82b4 => 165
	i32 3441283291, ; 533: Xamarin.AndroidX.DynamicAnimation.dll => 0xcd1dd0db => 240
	i32 3445260447, ; 534: System.Formats.Tar => 0xcd5a809f => 39
	i32 3452344032, ; 535: Microsoft.Maui.Controls.Compatibility.dll => 0xcdc696e0 => 190
	i32 3459815001, ; 536: Mapsui.Rendering.Skia => 0xce389659 => 179
	i32 3463511458, ; 537: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 306
	i32 3471940407, ; 538: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 17
	i32 3473156932, ; 539: SkiaSharp.Views.Maui.Controls.dll => 0xcf042b44 => 203
	i32 3476120550, ; 540: Mono.Android => 0xcf3163e6 => 169
	i32 3479583265, ; 541: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 319
	i32 3485117614, ; 542: System.Text.Json.dll => 0xcfbaacae => 210
	i32 3486566296, ; 543: System.Transactions => 0xcfd0c798 => 148
	i32 3493954962, ; 544: Xamarin.AndroidX.Concurrent.Futures.dll => 0xd0418592 => 229
	i32 3509114376, ; 545: System.Xml.Linq => 0xd128d608 => 153
	i32 3515174580, ; 546: System.Security.dll => 0xd1854eb4 => 130
	i32 3530912306, ; 547: System.Configuration => 0xd2757232 => 19
	i32 3539954161, ; 548: System.Net.HttpListener => 0xd2ff69f1 => 65
	i32 3542658132, ; 549: vi/Microsoft.Maui.Controls.resources => 0xd328ac54 => 325
	i32 3560100363, ; 550: System.Threading.Timer => 0xd432d20b => 145
	i32 3570554715, ; 551: System.IO.FileSystem.AccessControl => 0xd4d2575b => 47
	i32 3574246105, ; 552: BruTile.XmlSerializers.dll => 0xd50aaad9 => 171
	i32 3596930546, ; 553: de/Microsoft.Maui.Controls.resources => 0xd664cdf2 => 299
	i32 3597029428, ; 554: Xamarin.Android.Glide.GifDecoder.dll => 0xd6665034 => 215
	i32 3598340787, ; 555: System.Net.WebSockets.Client => 0xd67a52b3 => 79
	i32 3608519521, ; 556: System.Linq.dll => 0xd715a361 => 61
	i32 3623444314, ; 557: da/Microsoft.Maui.Controls.resources => 0xd7f95f5a => 298
	i32 3624195450, ; 558: System.Runtime.InteropServices.RuntimeInformation => 0xd804d57a => 106
	i32 3627220390, ; 559: Xamarin.AndroidX.Print.dll => 0xd832fda6 => 264
	i32 3633644679, ; 560: Xamarin.AndroidX.Annotation.Experimental => 0xd8950487 => 219
	i32 3638274909, ; 561: System.IO.FileSystem.Primitives.dll => 0xd8dbab5d => 49
	i32 3641597786, ; 562: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 250
	i32 3642401106, ; 563: VinhKhanhGuide.Core => 0xd91aa152 => 329
	i32 3643854240, ; 564: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 261
	i32 3645089577, ; 565: System.ComponentModel.DataAnnotations => 0xd943a729 => 14
	i32 3647796983, ; 566: pt-BR/Microsoft.Maui.Controls.resources => 0xd96cf6f7 => 316
	i32 3657292374, ; 567: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 182
	i32 3660523487, ; 568: System.Net.NetworkInformation => 0xda2f27df => 68
	i32 3662115805, ; 569: he/Microsoft.Maui.Controls.resources => 0xda4773dd => 304
	i32 3672681054, ; 570: Mono.Android.dll => 0xdae8aa5e => 169
	i32 3682565725, ; 571: Xamarin.AndroidX.Browser => 0xdb7f7e5d => 225
	i32 3684561358, ; 572: Xamarin.AndroidX.Concurrent.Futures => 0xdb9df1ce => 229
	i32 3686075795, ; 573: ms/Microsoft.Maui.Controls.resources => 0xdbb50d93 => 312
	i32 3697841164, ; 574: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 328
	i32 3700866549, ; 575: System.Net.WebProxy.dll => 0xdc96bdf5 => 78
	i32 3706696989, ; 576: Xamarin.AndroidX.Core.Core.Ktx.dll => 0xdcefb51d => 234
	i32 3716563718, ; 577: System.Runtime.Intrinsics => 0xdd864306 => 108
	i32 3718780102, ; 578: Xamarin.AndroidX.Annotation => 0xdda814c6 => 218
	i32 3724971120, ; 579: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 260
	i32 3732100267, ; 580: System.Net.NameResolution => 0xde7354ab => 67
	i32 3737834244, ; 581: System.Net.Http.Json.dll => 0xdecad304 => 63
	i32 3748608112, ; 582: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 27
	i32 3751444290, ; 583: System.Xml.XPath => 0xdf9a7f42 => 158
	i32 3786282454, ; 584: Xamarin.AndroidX.Collection => 0xe1ae15d6 => 227
	i32 3792276235, ; 585: System.Collections.NonGeneric => 0xe2098b0b => 10
	i32 3792835768, ; 586: HarfBuzzSharp => 0xe21214b8 => 175
	i32 3798102808, ; 587: BruTile => 0xe2627318 => 172
	i32 3800979733, ; 588: Microsoft.Maui.Controls.Compatibility => 0xe28e5915 => 190
	i32 3802395368, ; 589: System.Collections.Specialized.dll => 0xe2a3f2e8 => 11
	i32 3819260425, ; 590: System.Net.WebProxy => 0xe3a54a09 => 78
	i32 3823082795, ; 591: System.Security.Cryptography.dll => 0xe3df9d2b => 126
	i32 3829621856, ; 592: System.Numerics.dll => 0xe4436460 => 83
	i32 3841636137, ; 593: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 184
	i32 3844307129, ; 594: System.Net.Mail.dll => 0xe52378b9 => 66
	i32 3849253459, ; 595: System.Runtime.InteropServices.dll => 0xe56ef253 => 107
	i32 3870376305, ; 596: System.Net.HttpListener.dll => 0xe6b14171 => 65
	i32 3873536506, ; 597: System.Security.Principal => 0xe6e179fa => 128
	i32 3875112723, ; 598: System.Security.Cryptography.Encoding.dll => 0xe6f98713 => 122
	i32 3885497537, ; 599: System.Net.WebHeaderCollection.dll => 0xe797fcc1 => 77
	i32 3885922214, ; 600: Xamarin.AndroidX.Transition.dll => 0xe79e77a6 => 275
	i32 3888767677, ; 601: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller => 0xe7c9e2bd => 265
	i32 3889960447, ; 602: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 327
	i32 3896106733, ; 603: System.Collections.Concurrent.dll => 0xe839deed => 8
	i32 3896760992, ; 604: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 233
	i32 3901907137, ; 605: Microsoft.VisualBasic.Core.dll => 0xe89260c1 => 2
	i32 3920810846, ; 606: System.IO.Compression.FileSystem.dll => 0xe9b2d35e => 44
	i32 3921031405, ; 607: Xamarin.AndroidX.VersionedParcelable.dll => 0xe9b630ed => 278
	i32 3928044579, ; 608: System.Xml.ReaderWriter => 0xea213423 => 154
	i32 3930554604, ; 609: System.Security.Principal.dll => 0xea4780ec => 128
	i32 3931092270, ; 610: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 263
	i32 3934069706, ; 611: Topten.RichTextKit.dll => 0xea7d23ca => 211
	i32 3945713374, ; 612: System.Data.DataSetExtensions.dll => 0xeb2ecede => 23
	i32 3952289091, ; 613: NetTopologySuite.Features.dll => 0xeb932543 => 197
	i32 3953583589, ; 614: Svg.Skia => 0xeba6e5e5 => 207
	i32 3953953790, ; 615: System.Text.Encoding.CodePages => 0xebac8bfe => 133
	i32 3955647286, ; 616: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 221
	i32 3959773229, ; 617: Xamarin.AndroidX.Lifecycle.Process => 0xec05582d => 252
	i32 3980434154, ; 618: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 322
	i32 3987592930, ; 619: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 304
	i32 4003436829, ; 620: System.Diagnostics.Process.dll => 0xee9f991d => 29
	i32 4003906742, ; 621: HarfBuzzSharp.dll => 0xeea6c4b6 => 175
	i32 4013003792, ; 622: BruTile.dll => 0xef319410 => 172
	i32 4015948917, ; 623: Xamarin.AndroidX.Annotation.Jvm.dll => 0xef5e8475 => 220
	i32 4022681963, ; 624: Mapsui.Tiling => 0xefc5416b => 180
	i32 4023392905, ; 625: System.IO.Pipelines => 0xefd01a89 => 208
	i32 4025784931, ; 626: System.Memory => 0xeff49a63 => 62
	i32 4046471985, ; 627: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 192
	i32 4054681211, ; 628: System.Reflection.Emit.ILGeneration => 0xf1ad867b => 90
	i32 4066802364, ; 629: SkiaSharp.HarfBuzz => 0xf2667abc => 201
	i32 4068434129, ; 630: System.Private.Xml.Linq.dll => 0xf27f60d1 => 87
	i32 4070331268, ; 631: id/Microsoft.Maui.Controls.resources => 0xf29c5384 => 308
	i32 4073602200, ; 632: System.Threading.dll => 0xf2ce3c98 => 146
	i32 4094352644, ; 633: Microsoft.Maui.Essentials.dll => 0xf40add04 => 194
	i32 4099507663, ; 634: System.Drawing.dll => 0xf45985cf => 36
	i32 4100113165, ; 635: System.Private.Uri => 0xf462c30d => 86
	i32 4101593132, ; 636: Xamarin.AndroidX.Emoji2 => 0xf479582c => 241
	i32 4102112229, ; 637: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 317
	i32 4119206479, ; 638: pl/Microsoft.Maui.Controls.resources => 0xf5861a4f => 315
	i32 4125707920, ; 639: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 312
	i32 4126470640, ; 640: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 183
	i32 4127667938, ; 641: System.IO.FileSystem.Watcher => 0xf60736e2 => 50
	i32 4130442656, ; 642: System.AppContext => 0xf6318da0 => 6
	i32 4144557198, ; 643: NetTopologySuite.Features => 0xf708ec8e => 197
	i32 4147896353, ; 644: System.Reflection.Emit.ILGeneration.dll => 0xf73be021 => 90
	i32 4151237749, ; 645: System.Core => 0xf76edc75 => 21
	i32 4159265925, ; 646: System.Xml.XmlSerializer => 0xf7e95c85 => 160
	i32 4161255271, ; 647: System.Reflection.TypeExtensions => 0xf807b767 => 96
	i32 4164802419, ; 648: System.IO.FileSystem.Watcher.dll => 0xf83dd773 => 50
	i32 4181436372, ; 649: System.Runtime.Serialization.Primitives => 0xf93ba7d4 => 113
	i32 4182413190, ; 650: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 257
	i32 4185676441, ; 651: System.Security => 0xf97c5a99 => 130
	i32 4196529839, ; 652: System.Net.WebClient.dll => 0xfa21f6af => 76
	i32 4213026141, ; 653: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 27
	i32 4234116406, ; 654: pt/Microsoft.Maui.Controls.resources => 0xfc5f7d36 => 317
	i32 4256097574, ; 655: Xamarin.AndroidX.Core.Core.Ktx => 0xfdaee526 => 234
	i32 4258378803, ; 656: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx => 0xfdd1b433 => 256
	i32 4260525087, ; 657: System.Buffers => 0xfdf2741f => 7
	i32 4271975918, ; 658: Microsoft.Maui.Controls.dll => 0xfea12dee => 191
	i32 4274976490, ; 659: System.Runtime.Numerics => 0xfecef6ea => 110
	i32 4292120959, ; 660: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 257
	i32 4294763496 ; 661: Xamarin.AndroidX.ExifInterface.dll => 0xfffce3e8 => 243
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [662 x i32] [
	i32 68, ; 0
	i32 67, ; 1
	i32 108, ; 2
	i32 253, ; 3
	i32 287, ; 4
	i32 48, ; 5
	i32 80, ; 6
	i32 143, ; 7
	i32 296, ; 8
	i32 30, ; 9
	i32 124, ; 10
	i32 195, ; 11
	i32 102, ; 12
	i32 271, ; 13
	i32 326, ; 14
	i32 107, ; 15
	i32 271, ; 16
	i32 137, ; 17
	i32 291, ; 18
	i32 77, ; 19
	i32 207, ; 20
	i32 124, ; 21
	i32 13, ; 22
	i32 227, ; 23
	i32 132, ; 24
	i32 273, ; 25
	i32 149, ; 26
	i32 325, ; 27
	i32 326, ; 28
	i32 18, ; 29
	i32 225, ; 30
	i32 26, ; 31
	i32 247, ; 32
	i32 1, ; 33
	i32 59, ; 34
	i32 42, ; 35
	i32 91, ; 36
	i32 230, ; 37
	i32 145, ; 38
	i32 249, ; 39
	i32 246, ; 40
	i32 297, ; 41
	i32 54, ; 42
	i32 69, ; 43
	i32 216, ; 44
	i32 83, ; 45
	i32 310, ; 46
	i32 248, ; 47
	i32 309, ; 48
	i32 297, ; 49
	i32 131, ; 50
	i32 55, ; 51
	i32 147, ; 52
	i32 74, ; 53
	i32 143, ; 54
	i32 62, ; 55
	i32 144, ; 56
	i32 330, ; 57
	i32 163, ; 58
	i32 321, ; 59
	i32 231, ; 60
	i32 12, ; 61
	i32 244, ; 62
	i32 125, ; 63
	i32 150, ; 64
	i32 113, ; 65
	i32 173, ; 66
	i32 164, ; 67
	i32 162, ; 68
	i32 206, ; 69
	i32 246, ; 70
	i32 259, ; 71
	i32 84, ; 72
	i32 308, ; 73
	i32 302, ; 74
	i32 189, ; 75
	i32 200, ; 76
	i32 148, ; 77
	i32 305, ; 78
	i32 291, ; 79
	i32 60, ; 80
	i32 185, ; 81
	i32 51, ; 82
	i32 103, ; 83
	i32 114, ; 84
	i32 40, ; 85
	i32 284, ; 86
	i32 282, ; 87
	i32 120, ; 88
	i32 316, ; 89
	i32 52, ; 90
	i32 44, ; 91
	i32 119, ; 92
	i32 236, ; 93
	i32 242, ; 94
	i32 81, ; 95
	i32 209, ; 96
	i32 278, ; 97
	i32 223, ; 98
	i32 8, ; 99
	i32 179, ; 100
	i32 73, ; 101
	i32 296, ; 102
	i32 153, ; 103
	i32 293, ; 104
	i32 152, ; 105
	i32 92, ; 106
	i32 288, ; 107
	i32 45, ; 108
	i32 311, ; 109
	i32 299, ; 110
	i32 292, ; 111
	i32 109, ; 112
	i32 129, ; 113
	i32 25, ; 114
	i32 213, ; 115
	i32 72, ; 116
	i32 55, ; 117
	i32 46, ; 118
	i32 201, ; 119
	i32 188, ; 120
	i32 237, ; 121
	i32 22, ; 122
	i32 251, ; 123
	i32 86, ; 124
	i32 43, ; 125
	i32 158, ; 126
	i32 71, ; 127
	i32 264, ; 128
	i32 295, ; 129
	i32 3, ; 130
	i32 42, ; 131
	i32 63, ; 132
	i32 196, ; 133
	i32 16, ; 134
	i32 53, ; 135
	i32 323, ; 136
	i32 287, ; 137
	i32 105, ; 138
	i32 177, ; 139
	i32 292, ; 140
	i32 285, ; 141
	i32 248, ; 142
	i32 34, ; 143
	i32 156, ; 144
	i32 85, ; 145
	i32 32, ; 146
	i32 12, ; 147
	i32 327, ; 148
	i32 51, ; 149
	i32 301, ; 150
	i32 56, ; 151
	i32 268, ; 152
	i32 36, ; 153
	i32 184, ; 154
	i32 298, ; 155
	i32 286, ; 156
	i32 221, ; 157
	i32 35, ; 158
	i32 58, ; 159
	i32 255, ; 160
	i32 177, ; 161
	i32 174, ; 162
	i32 17, ; 163
	i32 289, ; 164
	i32 162, ; 165
	i32 254, ; 166
	i32 187, ; 167
	i32 281, ; 168
	i32 151, ; 169
	i32 277, ; 170
	i32 262, ; 171
	i32 0, ; 172
	i32 302, ; 173
	i32 315, ; 174
	i32 223, ; 175
	i32 29, ; 176
	i32 52, ; 177
	i32 313, ; 178
	i32 171, ; 179
	i32 282, ; 180
	i32 5, ; 181
	i32 272, ; 182
	i32 276, ; 183
	i32 228, ; 184
	i32 293, ; 185
	i32 220, ; 186
	i32 239, ; 187
	i32 85, ; 188
	i32 211, ; 189
	i32 281, ; 190
	i32 61, ; 191
	i32 112, ; 192
	i32 57, ; 193
	i32 268, ; 194
	i32 99, ; 195
	i32 176, ; 196
	i32 0, ; 197
	i32 19, ; 198
	i32 232, ; 199
	i32 111, ; 200
	i32 101, ; 201
	i32 102, ; 202
	i32 180, ; 203
	i32 104, ; 204
	i32 285, ; 205
	i32 71, ; 206
	i32 196, ; 207
	i32 38, ; 208
	i32 32, ; 209
	i32 103, ; 210
	i32 73, ; 211
	i32 9, ; 212
	i32 123, ; 213
	i32 46, ; 214
	i32 222, ; 215
	i32 189, ; 216
	i32 9, ; 217
	i32 43, ; 218
	i32 4, ; 219
	i32 269, ; 220
	i32 305, ; 221
	i32 300, ; 222
	i32 31, ; 223
	i32 136, ; 224
	i32 92, ; 225
	i32 93, ; 226
	i32 320, ; 227
	i32 303, ; 228
	i32 49, ; 229
	i32 139, ; 230
	i32 112, ; 231
	i32 138, ; 232
	i32 321, ; 233
	i32 238, ; 234
	i32 328, ; 235
	i32 115, ; 236
	i32 286, ; 237
	i32 199, ; 238
	i32 155, ; 239
	i32 76, ; 240
	i32 79, ; 241
	i32 258, ; 242
	i32 37, ; 243
	i32 203, ; 244
	i32 280, ; 245
	i32 242, ; 246
	i32 235, ; 247
	i32 64, ; 248
	i32 136, ; 249
	i32 15, ; 250
	i32 116, ; 251
	i32 274, ; 252
	i32 283, ; 253
	i32 230, ; 254
	i32 198, ; 255
	i32 48, ; 256
	i32 70, ; 257
	i32 80, ; 258
	i32 126, ; 259
	i32 94, ; 260
	i32 121, ; 261
	i32 290, ; 262
	i32 26, ; 263
	i32 251, ; 264
	i32 97, ; 265
	i32 28, ; 266
	i32 226, ; 267
	i32 318, ; 268
	i32 147, ; 269
	i32 208, ; 270
	i32 167, ; 271
	i32 4, ; 272
	i32 98, ; 273
	i32 33, ; 274
	i32 93, ; 275
	i32 273, ; 276
	i32 185, ; 277
	i32 21, ; 278
	i32 41, ; 279
	i32 168, ; 280
	i32 244, ; 281
	i32 258, ; 282
	i32 313, ; 283
	i32 289, ; 284
	i32 283, ; 285
	i32 263, ; 286
	i32 2, ; 287
	i32 307, ; 288
	i32 134, ; 289
	i32 111, ; 290
	i32 186, ; 291
	i32 178, ; 292
	i32 324, ; 293
	i32 213, ; 294
	i32 58, ; 295
	i32 329, ; 296
	i32 95, ; 297
	i32 39, ; 298
	i32 224, ; 299
	i32 25, ; 300
	i32 94, ; 301
	i32 89, ; 302
	i32 99, ; 303
	i32 10, ; 304
	i32 87, ; 305
	i32 100, ; 306
	i32 310, ; 307
	i32 270, ; 308
	i32 181, ; 309
	i32 290, ; 310
	i32 215, ; 311
	i32 7, ; 312
	i32 306, ; 313
	i32 255, ; 314
	i32 295, ; 315
	i32 212, ; 316
	i32 309, ; 317
	i32 88, ; 318
	i32 250, ; 319
	i32 152, ; 320
	i32 33, ; 321
	i32 116, ; 322
	i32 82, ; 323
	i32 20, ; 324
	i32 11, ; 325
	i32 160, ; 326
	i32 3, ; 327
	i32 311, ; 328
	i32 193, ; 329
	i32 318, ; 330
	i32 188, ; 331
	i32 186, ; 332
	i32 84, ; 333
	i32 294, ; 334
	i32 64, ; 335
	i32 277, ; 336
	i32 141, ; 337
	i32 259, ; 338
	i32 155, ; 339
	i32 41, ; 340
	i32 117, ; 341
	i32 182, ; 342
	i32 214, ; 343
	i32 303, ; 344
	i32 266, ; 345
	i32 322, ; 346
	i32 131, ; 347
	i32 75, ; 348
	i32 66, ; 349
	i32 170, ; 350
	i32 218, ; 351
	i32 141, ; 352
	i32 173, ; 353
	i32 106, ; 354
	i32 149, ; 355
	i32 70, ; 356
	i32 204, ; 357
	i32 154, ; 358
	i32 323, ; 359
	i32 181, ; 360
	i32 121, ; 361
	i32 127, ; 362
	i32 150, ; 363
	i32 241, ; 364
	i32 139, ; 365
	i32 314, ; 366
	i32 228, ; 367
	i32 20, ; 368
	i32 14, ; 369
	i32 135, ; 370
	i32 75, ; 371
	i32 59, ; 372
	i32 231, ; 373
	i32 165, ; 374
	i32 166, ; 375
	i32 191, ; 376
	i32 15, ; 377
	i32 74, ; 378
	i32 6, ; 379
	i32 23, ; 380
	i32 253, ; 381
	i32 320, ; 382
	i32 212, ; 383
	i32 205, ; 384
	i32 91, ; 385
	i32 1, ; 386
	i32 209, ; 387
	i32 178, ; 388
	i32 254, ; 389
	i32 276, ; 390
	i32 134, ; 391
	i32 69, ; 392
	i32 144, ; 393
	i32 206, ; 394
	i32 294, ; 395
	i32 205, ; 396
	i32 245, ; 397
	i32 187, ; 398
	i32 88, ; 399
	i32 96, ; 400
	i32 235, ; 401
	i32 240, ; 402
	i32 204, ; 403
	i32 31, ; 404
	i32 45, ; 405
	i32 249, ; 406
	i32 214, ; 407
	i32 109, ; 408
	i32 156, ; 409
	i32 35, ; 410
	i32 22, ; 411
	i32 114, ; 412
	i32 57, ; 413
	i32 274, ; 414
	i32 142, ; 415
	i32 118, ; 416
	i32 120, ; 417
	i32 110, ; 418
	i32 216, ; 419
	i32 137, ; 420
	i32 222, ; 421
	i32 54, ; 422
	i32 105, ; 423
	i32 198, ; 424
	i32 192, ; 425
	i32 193, ; 426
	i32 133, ; 427
	i32 288, ; 428
	i32 279, ; 429
	i32 267, ; 430
	i32 245, ; 431
	i32 202, ; 432
	i32 195, ; 433
	i32 324, ; 434
	i32 157, ; 435
	i32 301, ; 436
	i32 232, ; 437
	i32 161, ; 438
	i32 132, ; 439
	i32 267, ; 440
	i32 159, ; 441
	i32 314, ; 442
	i32 256, ; 443
	i32 138, ; 444
	i32 279, ; 445
	i32 275, ; 446
	i32 300, ; 447
	i32 167, ; 448
	i32 194, ; 449
	i32 217, ; 450
	i32 284, ; 451
	i32 40, ; 452
	i32 319, ; 453
	i32 243, ; 454
	i32 81, ; 455
	i32 56, ; 456
	i32 37, ; 457
	i32 97, ; 458
	i32 164, ; 459
	i32 170, ; 460
	i32 202, ; 461
	i32 280, ; 462
	i32 82, ; 463
	i32 219, ; 464
	i32 98, ; 465
	i32 30, ; 466
	i32 157, ; 467
	i32 18, ; 468
	i32 127, ; 469
	i32 119, ; 470
	i32 239, ; 471
	i32 270, ; 472
	i32 252, ; 473
	i32 272, ; 474
	i32 163, ; 475
	i32 247, ; 476
	i32 330, ; 477
	i32 269, ; 478
	i32 260, ; 479
	i32 168, ; 480
	i32 16, ; 481
	i32 142, ; 482
	i32 307, ; 483
	i32 125, ; 484
	i32 118, ; 485
	i32 38, ; 486
	i32 115, ; 487
	i32 47, ; 488
	i32 140, ; 489
	i32 117, ; 490
	i32 199, ; 491
	i32 34, ; 492
	i32 174, ; 493
	i32 95, ; 494
	i32 53, ; 495
	i32 261, ; 496
	i32 129, ; 497
	i32 151, ; 498
	i32 24, ; 499
	i32 159, ; 500
	i32 238, ; 501
	i32 146, ; 502
	i32 104, ; 503
	i32 89, ; 504
	i32 226, ; 505
	i32 60, ; 506
	i32 140, ; 507
	i32 100, ; 508
	i32 176, ; 509
	i32 5, ; 510
	i32 13, ; 511
	i32 122, ; 512
	i32 135, ; 513
	i32 28, ; 514
	i32 72, ; 515
	i32 236, ; 516
	i32 24, ; 517
	i32 200, ; 518
	i32 224, ; 519
	i32 265, ; 520
	i32 262, ; 521
	i32 210, ; 522
	i32 217, ; 523
	i32 233, ; 524
	i32 166, ; 525
	i32 266, ; 526
	i32 101, ; 527
	i32 123, ; 528
	i32 237, ; 529
	i32 183, ; 530
	i32 161, ; 531
	i32 165, ; 532
	i32 240, ; 533
	i32 39, ; 534
	i32 190, ; 535
	i32 179, ; 536
	i32 306, ; 537
	i32 17, ; 538
	i32 203, ; 539
	i32 169, ; 540
	i32 319, ; 541
	i32 210, ; 542
	i32 148, ; 543
	i32 229, ; 544
	i32 153, ; 545
	i32 130, ; 546
	i32 19, ; 547
	i32 65, ; 548
	i32 325, ; 549
	i32 145, ; 550
	i32 47, ; 551
	i32 171, ; 552
	i32 299, ; 553
	i32 215, ; 554
	i32 79, ; 555
	i32 61, ; 556
	i32 298, ; 557
	i32 106, ; 558
	i32 264, ; 559
	i32 219, ; 560
	i32 49, ; 561
	i32 250, ; 562
	i32 329, ; 563
	i32 261, ; 564
	i32 14, ; 565
	i32 316, ; 566
	i32 182, ; 567
	i32 68, ; 568
	i32 304, ; 569
	i32 169, ; 570
	i32 225, ; 571
	i32 229, ; 572
	i32 312, ; 573
	i32 328, ; 574
	i32 78, ; 575
	i32 234, ; 576
	i32 108, ; 577
	i32 218, ; 578
	i32 260, ; 579
	i32 67, ; 580
	i32 63, ; 581
	i32 27, ; 582
	i32 158, ; 583
	i32 227, ; 584
	i32 10, ; 585
	i32 175, ; 586
	i32 172, ; 587
	i32 190, ; 588
	i32 11, ; 589
	i32 78, ; 590
	i32 126, ; 591
	i32 83, ; 592
	i32 184, ; 593
	i32 66, ; 594
	i32 107, ; 595
	i32 65, ; 596
	i32 128, ; 597
	i32 122, ; 598
	i32 77, ; 599
	i32 275, ; 600
	i32 265, ; 601
	i32 327, ; 602
	i32 8, ; 603
	i32 233, ; 604
	i32 2, ; 605
	i32 44, ; 606
	i32 278, ; 607
	i32 154, ; 608
	i32 128, ; 609
	i32 263, ; 610
	i32 211, ; 611
	i32 23, ; 612
	i32 197, ; 613
	i32 207, ; 614
	i32 133, ; 615
	i32 221, ; 616
	i32 252, ; 617
	i32 322, ; 618
	i32 304, ; 619
	i32 29, ; 620
	i32 175, ; 621
	i32 172, ; 622
	i32 220, ; 623
	i32 180, ; 624
	i32 208, ; 625
	i32 62, ; 626
	i32 192, ; 627
	i32 90, ; 628
	i32 201, ; 629
	i32 87, ; 630
	i32 308, ; 631
	i32 146, ; 632
	i32 194, ; 633
	i32 36, ; 634
	i32 86, ; 635
	i32 241, ; 636
	i32 317, ; 637
	i32 315, ; 638
	i32 312, ; 639
	i32 183, ; 640
	i32 50, ; 641
	i32 6, ; 642
	i32 197, ; 643
	i32 90, ; 644
	i32 21, ; 645
	i32 160, ; 646
	i32 96, ; 647
	i32 50, ; 648
	i32 113, ; 649
	i32 257, ; 650
	i32 130, ; 651
	i32 76, ; 652
	i32 27, ; 653
	i32 317, ; 654
	i32 234, ; 655
	i32 256, ; 656
	i32 7, ; 657
	i32 191, ; 658
	i32 110, ; 659
	i32 257, ; 660
	i32 243 ; 661
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 4

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 4

; Functions

; Function attributes: "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 4, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+armv7-a,+d32,+dsp,+fp64,+neon,+vfp2,+vfp2sp,+vfp3,+vfp3d16,+vfp3d16sp,+vfp3sp,-aes,-fp-armv8,-fp-armv8d16,-fp-armv8d16sp,-fp-armv8sp,-fp16,-fp16fml,-fullfp16,-sha2,-thumb-mode,-vfp4,-vfp4d16,-vfp4d16sp,-vfp4sp" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+armv7-a,+d32,+dsp,+fp64,+neon,+vfp2,+vfp2sp,+vfp3,+vfp3d16,+vfp3d16sp,+vfp3sp,-aes,-fp-armv8,-fp-armv8d16,-fp-armv8d16sp,-fp-armv8sp,-fp16,-fp16fml,-fullfp16,-sha2,-thumb-mode,-vfp4,-vfp4d16,-vfp4d16sp,-vfp4sp" }

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"min_enum_size", i32 4}
