FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine as dotnet-build

# restore
WORKDIR /build-dir
COPY projectfiles.tar .
RUN tar -xvf projectfiles.tar
WORKDIR /build-dir/server/
RUN dotnet restore

# build
WORKDIR /build-dir
COPY . .
WORKDIR /build-dir/server/src/Newsgirl.Fetcher/
RUN dotnet publish -c Release -o /build -r linux-x64

FROM alpine

COPY --from=dotnet-build /build /app

ENTRYPOINT ["/app/newsgirl-fetcher"]
