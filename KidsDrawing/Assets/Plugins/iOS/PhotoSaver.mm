// ArtworkArchiver から呼ばれ、画像を写真アプリに保存する。
// 初回は写真への追加の許可を求める（Info.plist の NSPhotoLibraryAddUsageDescription）。
#import <UIKit/UIKit.h>

extern "C" void KidsDrawing_SaveImageToPhotos(const unsigned char* data, int length)
{
    NSData* imageData = [NSData dataWithBytes:data length:length];
    UIImage* image = [UIImage imageWithData:imageData];
    if (image == nil)
    {
        NSLog(@"KidsDrawing: could not decode image for Photos");
        return;
    }
    UIImageWriteToSavedPhotosAlbum(image, nil, nil, nil);
}
