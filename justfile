set windows-shell := ["pwsh.exe", "-NoLogo", "-Command"]

g_cli_args := " -v --lv-ver 2015"
lv_proj := "\"LabVIEW Source\\G CLI.lvproj\""

rust-test:
  cd rust-proxy && cargo fmt --check
  cd rust-proxy && cargo clippy
  cd rust-proxy && cargo test --lib

unit-test:
  g-cli {{g_cli_args}} vitester -- -r "lv-results.xml" {{lv_proj}}
  
build-integration-test:
  g-cli {{g_cli_args}} lvbuildspec -- -p {{lv_proj}} -b "CWD Test"
  g-cli {{g_cli_args}} lvbuildspec -- -p {{lv_proj}} -b "Echo Test"
  g-cli {{g_cli_args}} lvbuildspec -- -p {{lv_proj}} -b "Large Output Test"
  g-cli {{g_cli_args}} lvbuildspec -- -p {{lv_proj}} -b "Quit with Code Test"
  g-cli {{g_cli_args}} lvbuildspec -- -p {{lv_proj}} -b "Test In Packed Library"
  
integration-test:
  cd rust-proxy && cargo test --test '*' -- --test-threads=1
  
integration-test-with-build: build-integration-test integration-test 
