SHELL := pwsh.exe
.SHELLFLAGS := -Command

restore:
	dotnet restore ./src/

install_velopack:
	dotnet tool update -g vpk

velopack: install_velopack clean build
	vpk pack -u 'TooltipFix' -v '$(VERSION)' -e 'TooltipFix.exe' -o 'velopack' --packTitle 'Windows 11 Tooltip Fix' -p 'bin' --shortcuts 'StartMenuRoot' --framework net9-x64-desktop

clean:
	-rm -Recurse -ErrorAction SilentlyContinue bin
	-rm -Recurse -ErrorAction SilentlyContinue velopack

build:
	dotnet publish ./src/TooltipFix/ --runtime win-x64 --output ./bin/
	
help:
	$(info Usage: make <target>)
	$(info )
	$(info Targets: )
	$(info   build                 Build the application )
	$(info   install_velopack      Installs the toolset for auto-updates )
	$(info   velopack              Build the application with auto-updates )
	$(info   restore               Restores dependencies )
	$(info   clean                 Cleans build artifact directories )
	$(info   help                  Show this help message )