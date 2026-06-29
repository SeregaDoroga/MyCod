# MyCod

Решение содержит три консольные программы на C# и общий проект с логикой. Код сделан кроссплатформенным: он использует .NET SDK, стандартную библиотеку .NET и не зависит от Visual Studio, Rider, VS Code или конкретной ОС.

## Нужно ли учитывать IDE, компилятор и ОС

Синтаксис C# не зависит от IDE. Один и тот же код можно открыть в Visual Studio на Windows, Rider, VS Code или собрать из терминала на Linux.

Что действительно важно:

- версия SDK/компилятора: проект нацелен на `net8.0`, поэтому нужен .NET SDK 8.0 или новее, который умеет собирать `net8.0`;
- оболочка терминала: в Windows используются `\` и кавычки PowerShell/CMD, в Linux обычно `/` и Bash;
- кодировки файлов: лог-стандартизатор читает UTF-8 с автоопределением BOM и пишет UTF-8 без BOM;
- системно-зависимые API: в решении они не используются, поэтому исходный код одинаковый для Windows и Linux.

Официальные страницы установки .NET:

- Windows: <https://learn.microsoft.com/dotnet/core/install/windows>
- Linux: <https://learn.microsoft.com/dotnet/core/install/linux>
- Ubuntu: <https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install>
- dotnet CLI: <https://learn.microsoft.com/dotnet/core/tools/>

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
- во время записи read-lock не выдается, поэтому читатели ждут окончания записи.

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

## Публикация исполняемых файлов

Если нужно получить отдельную папку с исполняемым файлом под конкретную ОС:

```bash
dotnet publish src/Task1.Compression -c Release -r win-x64 --self-contained true
dotnet publish src/Task2.ConcurrentServer -c Release -r linux-x64 --self-contained true
dotnet publish src/Task3.LogStandardizer -c Release -r linux-x64 --self-contained true
```

Runtime identifiers можно менять, например `win-x64`, `linux-x64`, `linux-arm64`.
