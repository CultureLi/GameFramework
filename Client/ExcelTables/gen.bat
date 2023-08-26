set GEN_CLIENT=dotnet ..\Tools\Luban.ClientServer\Luban.ClientServer.dll

%GEN_CLIENT% -j cfg --^
 -d Defines\__root__.xml ^
 --input_data_dir Datas ^
 --output_data_dir ../Assets/BundleRes/Config ^
 --output_code_dir ../Assets/GameMain/Runtime/Config ^
 --gen_types code_cs_unity_bin,data_bin ^
 -s all
pause