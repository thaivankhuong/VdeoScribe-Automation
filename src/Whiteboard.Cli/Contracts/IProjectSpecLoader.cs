using Whiteboard.Core.Models;

namespace Whiteboard.Cli.Contracts;

public interface IProjectSpecLoader
{
    VideoProject Load(string specPath);
}
