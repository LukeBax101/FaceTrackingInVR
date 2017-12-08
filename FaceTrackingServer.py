import os, time, sys
import win32pipe, win32file
import array
from tkinter import * 
from PIL import Image
from PIL import ImageDraw
from PIL import ImageFont
import io
import cv2
import numpy as np
import threading
import time
import struct

def loop():
    global first,newimg
    while(first == False):
        cv2.imshow('image',newimg)
        key = cv2.waitKey(1)



def showChunks(byteData):
    print("Header: " + str(hex(byteData[0]))+" "+str(hex(byteData[1]))+" "+str(hex(byteData[2]))+" "+str(hex(byteData[3]))+" "+str(hex(byteData[4]))+" "+str(hex(byteData[5]))+" "+str(hex(byteData[6]))+" "+str(hex(byteData[7])))
    end = ""
    pos = 8
    while( end != "IEND"):
        print("Chunk: ")
        length = (byteData[pos]<<24)+(byteData[pos+1]<<16)+(byteData[pos+2]<<8)+(byteData[pos+3])
        print("   Length: " + str(length))
        end = chr(byteData[pos+4])+ chr(byteData[pos+5])+ chr(byteData[pos+6])+ chr(byteData[pos+7])
        print("   Name: " + end)
        print("   CVC: "+ str(hex(byteData[pos+8+length]))+" "+str(hex(byteData[pos+9+length]))+" "+str(hex(byteData[pos+10+length]))+" "+str(hex(byteData[pos+11+length])))
        print("")
        pos = pos +12+length
    print("Position: " +str(pos))
    print("Length: " + str(len(byteData)))


def getLast(byteData):
    lastStartPos = 0
    final = False
    while (final == False):
        print("Header: " + str(hex(byteData[lastStartPos]))+" "+str(hex(byteData[lastStartPos+1]))+" "+str(hex(byteData[lastStartPos+2]))+" "+str(hex(byteData[lastStartPos+3]))+" "+str(hex(byteData[lastStartPos+4]))+" "+str(hex(byteData[lastStartPos+5]))+" "+str(hex(byteData[lastStartPos+6]))+" "+str(hex(byteData[lastStartPos+7])))
        end = ""
        pos = lastStartPos + 8
        while( end != "IEND"):
            print("Chunk: ")
            length = (byteData[pos]<<24)+(byteData[pos+1]<<16)+(byteData[pos+2]<<8)+(byteData[pos+3])
            print("   Length: " + str(length))
            end = chr(byteData[pos+4])+ chr(byteData[pos+5])+ chr(byteData[pos+6])+ chr(byteData[pos+7])
            print("   Name: " + end)
            print("   CVC: "+ str(hex(byteData[pos+8+length]))+" "+str(hex(byteData[pos+9+length]))+" "+str(hex(byteData[pos+10+length]))+" "+str(hex(byteData[pos+11+length])))
            print("")
            pos = pos +12+length
        print("Position: " +str(pos))
        print("Length: " + str(len(byteData)))
        
        if (pos == len(byteData)):
            final=True
        else:
            lastStartPos = pos
    return lastStartPos
    

face_cascade = cv2.CascadeClassifier('D:/Documents/University/CompSci/FaceDefault.xml')
eye_cascade = cv2.CascadeClassifier('D:/Documents/University/CompSci/Eye.xml')

p = win32pipe.CreateNamedPipe(r'\\.\pipe\OutgoingFrame',
    win32pipe.PIPE_ACCESS_DUPLEX,
    win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_WAIT,
    1, 65536, 65536,300,None)

q = win32pipe.CreateNamedPipe(r'\\.\pipe\IncomingData',
    win32pipe.PIPE_ACCESS_DUPLEX,
    win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_WAIT,
    1, 65536, 65536,300,None)

win32pipe.ConnectNamedPipe(p, None)
win32pipe.ConnectNamedPipe(q, None)

first = True
newimg = 0
#time.sleep(15)
while (True):
    
    data = win32file.ReadFile(p, 112608000)
    byteData = list(data[1])

    #print(getLast(byteData))

    
    #image = Image.open(io.BytesIO(byteData))
    
    stream = io.BytesIO(data[1])

    imgage = Image.open(stream)
    #draw = ImageDraw.Draw(img)
    #font = ImageFont.truetype("arial.ttf",14)
    #draw.text((0, 220),"This is a test11",(255,255,0),font=font)
    #draw = ImageDraw.Draw(img)
    #img.save("a_test.png")
    
    #root = Tk()
    #canvas = Canvas(root, width=500, height=500)
    #canvas.pack()
    # = PhotoImage(imgage)
    #canvas.create_image(250, 250, image=imgage)
    #root.mainloop()


    # Convert rawImage to Mat
    #pilImage = Image.open(StringIO(rawImage));
    # Create a black image
    #img = np.zeros((512,512,3), np.uint8)

    # Draw a diagonal blue line with thickness of 5 px
    #cv2.line(img,(0,0),(511,511),(255,0,0),5)

    img = np.array(imgage)
    RGB_img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    newimg = cv2.flip( RGB_img, 0 )

    
    #cv2.rectangle(newimg,(50,50),(100,100),(0,255,0),1)

    gray = cv2.cvtColor(newimg, cv2.COLOR_BGR2GRAY)
    
    faces = face_cascade.detectMultiScale(gray, 1.3, 5)

    outdata = [len(faces)]
    print("Faces: " + str(faces))
    for (x,y,w,h) in faces:
        cv2.rectangle(newimg,(x,y),(x+w,y+h),(255,0,0),2)
        roi_gray = gray[y:y+h, x:x+w]
        roi_color = newimg[y:y+h, x:x+w]
        eyes = eye_cascade.detectMultiScale(roi_gray)
        for (ex,ey,ew,eh) in eyes:
            cv2.rectangle(roi_color,(ex,ey),(ex+ew,ey+eh),(0,255,0),2)
        outdata = outdata + [x,y,w,h]
    #faces = []
    #b = bytearray(outdata,"ascii")
    fmt = ""
    for i in range(len(outdata)):
        fmt = fmt + "i"
    print(fmt)
    bi = struct.pack(fmt,*outdata)
    #print("Sent: " +outdata)
    win32file.WriteFile(q, bi)
    #rows = img.shape[0]
    #cols = img.shape[1]

    #M = cv2.getRotationMatrix2D((cols/2,rows/2),180,1)
    #dst = cv2.warpAffine(img,M,(cols,rows))
    

    #show it
    #cv2.NamedWindow('display')
    #cv2.MoveWindow('display', 10, 10)
    #cv2.ShowImage('display', img)
    #cv.WaitKey(0)
    #newimg = cv2.flip( dst, 1 )

    if (first):
        #cv2.destroyAllWindows()
        first = False
        threading.Thread(target=loop).start()
    
    #while():
   #     cv2.imshow('image',newimg)
     #   cv2.waitKey(1)

        
    #cv2.destroyAllWindows()
    #i = i+1
    #print("Here")

    
    #if data[0] == 0:
        #byteData = data[1].decode('utf-32',"backslashreplace")
        #print(byteData[0])
        #print(data[1][0:100])
        #b_array = bytearray(data[1],'ascii',"backslashreplace")
        #for elem in b_array:
            #print(elem)
    #else:
      #print('ERROR', data[0])
