using TorchSharp;

bool predict(int img)
{
    var device = torch.cuda.is_available() ? torch.device("cuda:0") : torch.device("cpu");
}