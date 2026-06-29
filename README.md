# MyCod

Решение содержит три консольные программы на C# и общий проект с логикой. Код кроссплатформенный: он использует .NET SDK, стандартную библиотеку .NET и не зависит от Visual Studio, Rider, VS Code или конкретной операционной системы.

## Нужно ли учитывать IDE, компилятор и ОС

Синтаксис C# не зависит от IDE. Один и тот же исходный код можно открыть в Visual Studio на Windows, Rider, VS Code или собрать из терминала на Linux.

Что действительно важно:

- версия SDK/компилятора: проект нацелен на `net8.0`, поэтому нужен .NET SDK 8.0 или новее;
- оболочка терминала: в Windows обычно используются `\` и правила PowerShell/CMD, в Linux обычно `/` и Bash;
- кодировки файлов: лог-стандартизатор читает UTF-8 с автоопределением BOM и пишет UTF-8 без BOM;
- системно-зависимые API: в решении они не используются, поэтому исходный код одинаковый для Windows и Linux.

Официальные страницы установки .NET:

- Windows: <https://learn.microsoft.com/dotnet/core/install/windows>
- Linux: <https://learn.microsoft.com/dotnet/core/install/linux>
- Ubuntu: <https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install>
- dotnet CLI: <https://learn.microsoft.com/dotnet/core/tools/>

## Что такое CI-статус

CI означает Continuous Integration, то есть автоматическая проверка кода после коммита или pull request. Например, GitHub Actions может автоматически выполнить:

- `dotnet restore`
- `dotnet build`
- `dotnet test` или другой тестовый запуск

CI-статус на GitHub показывает результат таких проверок: прошли они, упали или ещё выполняются. В этом репозитории workflow-файлов GitHub Actions сейчас нет, поэтому у коммита нет CI-статусов. Это не ошибка C#-проекта, а просто означает, что автоматические проверки на стороне GitHub не настроены. Проверки для этого решения выполнены вручную командами из раздела ниже.

## Структура

```text
src/MyCod.Core              общая библиотека
src/Task1.Compression       задача 1, компрессия и декомпрессия
src/Task2.ConcurrentServer  задача 2, потокобезопасный Server
src/Task3.LogStandardizer   задача 3, стандартизация логов
tests/MyCod.Tests           консольный набор проверок без внешних пакетов
samples/                    пример входного лога и ожидаемых файлов
```

## Установка .NET SDK

### Windows 10

Через winget:

```powershell
winget install Microsoft.DotNet.SDK.8
dotnet --info
```

Если `winget` недоступен, скачайте установщик SDK 8.0 или новее со страницы Microsoft Learn для Windows.

### Ubuntu или Debian

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
dotnet --info
```

Если пакет не найден, подключите репозиторий Microsoft или встроенный feed вашего дистрибутива по официальной инструкции для вашей версии ОС.

### Fedora

```bash
sudo dnf install -y dotnet-sdk-8.0
dotnet --info
```

### CentOS, RHEL или совместимые дистрибутивы

```bash
sudo dnf install -y dotnet-sdk-8.0
dotnet --info
```

На старых CentOS вместо `dnf` может использоваться `yum`, а пакет SDK может потребовать подключение Microsoft package repository.

## Сборка и проверка

Для внутреннего использования обычно достаточно скачать репозиторий, установить .NET SDK и запускать программы через `dotnet run`.

Из корня репозитория:

```bash
dotnet restore
dotnet build MyCod.sln -c Release
dotnet run --project tests/MyCod.Tests -c Release
```

## Задача 1: компрессия строки

Сжать строку:

```bash
dotnet run --project src/Task1.Compression -c Release -- compress aaabbcccdde
```

Ожидаемый вывод:

```text
a3b2c3d2e
```

Восстановить строку:

```bash
dotnet run --project src/Task1.Compression -c Release -- decompress a3b2c3d2e
```

Ожидаемый вывод:

```text
aaabbcccdde
```

## Задача 2: параллельный доступ к Server

Класс `Server` находится в `MyCod.Core.ConcurrentServer`. Он использует `ReaderWriterLockSlim`:

- `GetCount()` входит в read-lock, поэтому несколько читателей читают параллельно;
- `AddToCount(int value)` входит в write-lock, поэтому писатели выполняются строго последовательно;
- во время записи read-lock не выдаётся, поэтому читатели ждут окончания записи.

Демонстрационный запуск:

```bash
dotnet run --project src/Task2.ConcurrentServer -c Release
```

С настраиваемой нагрузкой:

```bash
dotnet run --project src/Task2.ConcurrentServer -c Release -- --readers 50 --writers 8 --iterations 20000
```

## Задача 3: стандартизация лог-файлов

Базовый запуск:

```bash
dotnet run --project src/Task3.LogStandardizer -c Release -- samples/input.log outputs/standardized.log
```

Программа создаст:

- `outputs/standardized.log` - валидные строки в едином формате с табуляцией;
- `outputs/problems.txt` - исходные строки, которые не подошли ни под один формат.

Можно явно задать путь к файлу проблем:

```bash
dotnet run --project src/Task3.LogStandardizer -c Release -- samples/input.log outputs/standardized.log --problems outputs/problems.txt
```

В условии есть противоречие: текст говорит про дату `DD-MM-YYYY`, но оба примера выходных строк используют `YYYY-MM-DD`. Поэтому значение по умолчанию совпадает с примерами: `yyyy-MM-dd`.

Если нужна строгая дата день-месяц-год:

```bash
dotnet run --project src/Task3.LogStandardizer -c Release -- samples/input.log outputs/standardized.log --date-format dd-MM-yyyy
```

## Необязательная публикация исполняемых файлов

`dotnet publish` не нужен, если пользователь просто скачивает проект для внутреннего использования и запускает его через .NET SDK. В этом случае достаточно команд из раздела "Сборка и проверка".

Публикация нужна в другом сценарии: когда требуется получить отдельную папку с готовым исполняемым файлом под конкретную ОС. Например, чтобы передать программу на компьютер, где не установлен .NET SDK. Это не установщик и не "размещение в интернет", а локальная упаковка результата сборки.

Примеры:

```bash
dotnet publish src/Task1.Compression -c Release -r win-x64 --self-contained true
dotnet publish src/Task2.ConcurrentServer -c Release -r linux-x64 --self-contained true
dotnet publish src/Task3.LogStandardizer -c Release -r linux-x64 --self-contained true
```

Что означают параметры:

- `-c Release` - собрать оптимизированную release-версию;
- `-r win-x64` или `-r linux-x64` - выбрать ОС и архитектуру, под которые готовится папка;
- `--self-contained true` - положить рядом с программой нужный runtime .NET, чтобы на целевой машине не требовалась установленная .NET Runtime.

Runtime identifiers можно менять, например `win-x64`, `linux-x64`, `linux-arm64`.
