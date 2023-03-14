# sms-api

## .net core 2.1

## sql server 2017

## Migrate database

Vào sql server, tạo database tên là `rent_code_`

Vào source code chạy `dotnet ef database update`

## Seeding data

/swagger/index.html#/DevSeed

## Run

Vào visual studio -> F5


## Environment

### Staging:

https://rcapi.httechs.net/

#### Deploy

1. Build package then push to repo https://github.com/hdthuan/sms-deploy, branch: `staging`.

2. Remote to VPS (staging)

3. Stop website `rentcode` in IIS.

4. Open command line then use git to pull the package

```
cd C:\inetpub\wwwroot\rentcode
git pull
```

### Production:

https://api.rentcode.net/

#### Deploy

1. Build package then push to repo https://github.com/hdthuan/sms-deploy, branch: `master`.

2. Remote to VPS (production)

3. Stop website `api.rentcode.net` in IIS.

4. Open command line then use git to pull the package

```
cd C:\web\api.rentcode.net
git pull
```
